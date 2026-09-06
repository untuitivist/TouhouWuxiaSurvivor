import gzip
import hashlib
import io
import json
from pathlib import Path
import tarfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import uuid

import activate_deployment as deployment


class DeploymentTests(unittest.TestCase):
    def setUp(self):
        self.work = Path(__file__).resolve().parents[2] / 'artifacts/deployment-tests' / uuid.uuid4().hex
        self.work.mkdir(parents=True)
        self.root = self.work / 'site'
        self.main = self.work / 'Caddyfile'
        self.snippet = self.work / 'active.caddy'
        self.new_snippet = self.work / 'new.caddy'
        self.main.write_text('allinagent.top, www.allinagent.top {\n handle { reverse_proxy 127.0.0.1:8080 }\n}\n', encoding='utf-8')
        self.new_snippet.write_text('handle /TouhouSurvivor/* { respond 200 }\n', encoding='utf-8')
        self.original = self.main.read_text(encoding='utf-8')
        self.arguments = SimpleNamespace(release='alpha-0.0.9-fixture', archive=self.work / 'site.tar.gz', snippet=self.new_snippet, config_sha256=deployment.sha256(self.main))
        self.html = '<script src="index.js"></script>\n<link href="index.icon.png">\nconst GODOT_CONFIG = {"args":[],"executable":"index","fileSizes":{"index.wasm":7}};\n'
        self.files = {'index.html': self.html.encode(), 'index.wasm': b'fixture'}
        self.prepare()

    def prepare(self):
        metadata = {'releaseId': self.arguments.release, 'sourceCommit': 'fixture', 'files': [{'name': name, 'bytes': len(content), 'sha256': hashlib.sha256(content).hexdigest()} for name, content in self.files.items()]}
        with tarfile.open(self.arguments.archive, 'w:gz') as archive:
            for name, content in {**self.files, 'deployment.json': json.dumps(metadata).encode()}.items():
                member = tarfile.TarInfo(name)
                member.size = len(content)
                archive.addfile(member, io.BytesIO(content))
        self.arguments.sha256 = deployment.sha256(self.arguments.archive)

    def activate(self, response=None):
        if response is None:
            response = ('/TouhouSurvivor/releases/' + self.arguments.release + '/').encode()
        with patch.object(deployment, 'ROOT', self.root), patch.object(deployment, 'MAIN', self.main), patch.object(deployment, 'SNIPPET', self.snippet), patch.object(deployment, 'run'), patch.object(deployment.subprocess, 'check_output', return_value=response):
            deployment.activate(self.arguments)

    def test_versioned_entry_and_preserved_original_artifacts(self):
        self.activate()
        entry = (self.root / 'public/index.html').read_text(encoding='utf-8')
        self.assertIn('/TouhouSurvivor/releases/alpha-0.0.9-fixture/index.js', entry)
        self.assertIn('"executable":"/TouhouSurvivor/releases/alpha-0.0.9-fixture/index"', entry)
        release = self.root / 'public/releases' / self.arguments.release
        self.assertEqual((release / 'index.html').read_bytes(), self.files['index.html'])
        self.assertEqual(gzip.decompress((release / 'index.wasm.gz').read_bytes()), b'fixture')
        self.assertEqual((self.root / 'backups' / self.arguments.release / 'Caddyfile').read_text(encoding='utf-8'), self.original)
        self.assertIn('reverse_proxy 127.0.0.1:8080', self.main.read_text(encoding='utf-8'))

    def test_config_race_aborts_before_modifying_site(self):
        self.main.write_text(self.original + '\n', encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'configuration changed'):
            self.activate()
        self.assertFalse(self.root.exists())

    def test_upload_corruption_aborts_before_modifying_site(self):
        self.arguments.sha256 = '0' * 64
        with self.assertRaisesRegex(ValueError, 'Upload checksum'):
            self.activate()
        self.assertFalse(self.root.exists())

    def test_archive_path_escape_rejected(self):
        self.files['../escape.txt'] = b'not allowed'
        self.prepare()
        with self.assertRaisesRegex(ValueError, 'Unsafe artifact name'):
            self.activate()
        self.assertEqual(self.main.read_text(encoding='utf-8'), self.original)
        self.assertFalse((self.root / 'public/releases/escape.txt').exists())

    def test_failed_health_check_restores_existing_site(self):
        entry = self.root / 'public/index.html'
        entry.parent.mkdir(parents=True)
        entry.write_text('previous entry', encoding='utf-8')
        self.snippet.write_text('previous route', encoding='utf-8')
        with self.assertRaisesRegex(RuntimeError, 'expected release'):
            self.activate(b'wrong site')
        self.assertEqual(entry.read_text(encoding='utf-8'), 'previous entry')
        self.assertEqual(self.snippet.read_text(encoding='utf-8'), 'previous route')
        self.assertEqual(self.main.read_text(encoding='utf-8'), self.original)


if __name__ == '__main__':
    unittest.main(verbosity=2)
