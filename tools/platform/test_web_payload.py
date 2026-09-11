import base64
import hashlib
import json
from pathlib import Path
import struct
import unittest
import uuid

import optimize_web_payload as payload


def fixture_pack(extra=None):
    data = {
        payload.RUNTIME + "libmonosgen-2.0.a": b"!<arch>\n" + b"already linked runtime" * 128,
        payload.RUNTIME + "Game.dll": b"managed gameplay",
        payload.RUNTIME + "icudt_CJK.dat": b"all globalization data retained",
        "assets/audio/bgm.ogg": b"unchanged music",
        "assets/image.a": b"not a runtime static library",
        "assets/text.txt": "中文语言数据".encode("utf-8"),
    }
    data.update(extra or {})
    manifest = "".join(path.removeprefix(payload.RUNTIME) + "\t" + base64.b64encode(hashlib.sha512(content).digest()).decode("ascii") + "\n"
                       for path, content in data.items() if path.startswith(payload.RUNTIME))
    data[payload.MANIFEST] = manifest.encode("utf-8")
    header = bytearray(112)
    header[:4] = b"GDPC"
    struct.pack_into("<IIIIIQQ", header, 4, 3, 4, 6, 1, 2, 112, 0)
    return payload.encode_pack(header, [payload.Entry(path, index * 16, content) for index, (path, content) in enumerate(data.items())])


class WebPayloadTests(unittest.TestCase):
    def test_only_linked_runtime_archives_are_excluded(self):
        original = fixture_pack()
        optimized, report = payload.optimize_pack(original)
        self.assertEqual(len(report["removed"]), 1)
        self.assertLess(len(optimized), len(original))
        self.assertTrue(report["retainedPayloadsUnchanged"])
        _, before = payload.parse_pack(original)
        _, after = payload.parse_pack(optimized)
        retained = {entry.path: entry.data for entry in after}
        for entry in before:
            if entry.path != payload.MANIFEST and not entry.path.endswith("libmonosgen-2.0.a"):
                self.assertEqual(retained[entry.path], entry.data)
        self.assertNotIn(b"libmonosgen-2.0.a", retained[payload.MANIFEST])
        self.assertIn(b"icudt_CJK.dat", retained[payload.MANIFEST])

    def test_optimizer_is_idempotent(self):
        optimized, _ = payload.optimize_pack(fixture_pack())
        repeated, report = payload.optimize_pack(optimized)
        self.assertEqual(repeated, optimized)
        self.assertEqual(report["removed"], [])
        self.assertFalse(report["publishManifestUpdated"])

    def test_nested_and_non_runtime_archives_are_not_guessed_away(self):
        packed = fixture_pack({payload.RUNTIME + "custom/plugin.a": b"custom plugin"})
        optimized, _ = payload.optimize_pack(packed)
        _, entries = payload.parse_pack(optimized)
        self.assertIn(payload.RUNTIME + "custom/plugin.a", {entry.path for entry in entries})

    def test_changed_pack_data_is_rejected(self):
        packed = bytearray(fixture_pack())
        packed[120] ^= 1
        with self.assertRaisesRegex(ValueError, "checksum mismatch"):
            payload.optimize_pack(packed)

    def test_wrong_version_flags_header_and_truncation_are_rejected(self):
        for offset, value in [(4, 2), (8, 5), (24, 111), (20, 3), (40, 1), (32, 1)]:
            with self.subTest(offset=offset):
                packed = bytearray(fixture_pack())
                struct.pack_into("<I", packed, offset, value)
                with self.assertRaises(ValueError):
                    payload.optimize_pack(packed)
        for cut in [1, 40, 120]:
            with self.assertRaises(ValueError):
                payload.optimize_pack(fixture_pack()[:-cut])

    def test_trailing_data_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "trailing"):
            payload.optimize_pack(fixture_pack() + b"unrecognized trailer")

    def test_unknown_archive_contents_are_rejected(self):
        with self.assertRaisesRegex(ValueError, "not a static archive"):
            payload.optimize_pack(fixture_pack({payload.RUNTIME + "custom.a": b"not an archive"}))

    def test_paths_cannot_escape_or_alias(self):
        for path in ["../outside", "/absolute", "assets//duplicate", "assets/./file", "C:/absolute", "assets\\file"]:
            with self.subTest(path=path), self.assertRaisesRegex(ValueError, "path"):
                payload.optimize_pack(fixture_pack({path: b"invalid"}))

    def test_publish_manifest_is_verified(self):
        header, entries = payload.parse_pack(fixture_pack())
        for replacement in [b"missing delimiter\n", b"missing.dll\tbad-hash\n", entries[-1].data.replace(b"Game.dll", b"Gone.dll")]:
            modified = [payload.Entry(entry.path, entry.offset, replacement) if entry.path == payload.MANIFEST else entry for entry in entries]
            with self.assertRaises(ValueError):
                payload.optimize_pack(payload.encode_pack(header, modified))
        without_manifest = [entry for entry in entries if entry.path != payload.MANIFEST]
        with self.assertRaisesRegex(ValueError, "publish manifest"):
            payload.optimize_pack(payload.encode_pack(header, without_manifest))

    def test_site_preserves_input_and_updates_exact_download_size(self):
        work = self.prepare_site()
        source, output, report_path = work / "source", work / "output", work / "report.json"
        before = {item.name: item.read_bytes() for item in source.iterdir()}
        report = payload.optimize_site(source, output, report_path)
        self.assertEqual(before, {item.name: item.read_bytes() for item in source.iterdir()})
        self.assertEqual((source / "index.wasm").read_bytes(), (output / "index.wasm").read_bytes())
        self.assertEqual((source / "index.loader.js").read_bytes(), (output / "index.loader.js").read_bytes())
        configuration = json.loads((output / "index.html").read_text(encoding="utf-8").split("const GODOT_CONFIG = ")[1].split(";", 1)[0])
        self.assertEqual(configuration["fileSizes"]["index.pck"], (output / "index.pck").stat().st_size)
        self.assertEqual(configuration["args"], [])
        self.assertLess(report["coreGzipBytesAfter"], report["coreGzipBytesBefore"])
        self.assertFalse(report_path.read_bytes().startswith(b"\xef\xbb\xbf"))
        with self.assertRaisesRegex(ValueError, "immutable"):
            payload.optimize_site(source, output, work / "other-report.json")

    def test_existing_input_and_report_locations_cannot_be_written(self):
        work = self.prepare_site()
        source = work / "source"
        for output, report in [(source, work / "report.json"), (source / "nested", work / "report.json"), (work / "output", source / "report.json")]:
            with self.assertRaisesRegex(ValueError, "immutable"):
                payload.optimize_site(source, output, report)

    def test_mismatched_shell_fails_before_output_creation(self):
        work = self.prepare_site()
        source = work / "source"
        (source / "index.html").write_text("const GODOT_CONFIG = {};", encoding="utf-8")
        with self.assertRaises(ValueError):
            payload.optimize_site(source, work / "output", work / "report.json")
        self.assertFalse((work / "output").exists())

    def test_precompressed_input_is_rejected(self):
        work = self.prepare_site()
        (work / "source/index.pck.gz").write_bytes(b"stale compression")
        with self.assertRaisesRegex(ValueError, "before creating compressed"):
            payload.optimize_site(work / "source", work / "output", work / "report.json")

    def prepare_site(self):
        work = Path(__file__).resolve().parents[2] / "artifacts/web-payload-tests" / uuid.uuid4().hex
        source = work / "source"
        source.mkdir(parents=True)
        packed, wasm = fixture_pack(), b"unchanged engine"
        (source / "index.pck").write_bytes(packed)
        (source / "index.wasm").write_bytes(wasm)
        (source / "index.loader.js").write_text("unchanged loader", encoding="utf-8")
        config = {"args": [], "executable": "index", "fileSizes": {"index.pck": len(packed), "index.wasm": len(wasm)}}
        (source / "index.html").write_text("const GODOT_CONFIG = " + json.dumps(config) + ";\nconst TOUHOU_DOWNLOADS = null;\n", encoding="utf-8")
        return work


if __name__ == "__main__":
    unittest.main()
