import ast
import hashlib
import json
import pathlib
import shutil
import subprocess
import sys


def write_text(target, text):
    target.parent.mkdir(parents=True, exist_ok=True)
    if not target.exists() or target.read_text(encoding='utf-8') != text:
        target.write_text(text, encoding='utf-8')


def digest(target):
    return hashlib.sha256(target.read_bytes()).hexdigest()


def replace_once(text, before, after):
    if text.count(before) != 1:
        raise RuntimeError('Expected exact integration context: ' + before)
    return text.replace(before, after)


def main():
    repository = pathlib.Path(sys.argv[1]).resolve()
    workspace = pathlib.Path(sys.argv[2]).resolve()
    expected = pathlib.Path.home() / '.cache/touhou-webgpu-mono-f329e39c-b9498598'
    if workspace != expected.resolve():
        raise RuntimeError('Unexpected isolated engine workspace: ' + str(workspace))
    baseline = pathlib.Path.home() / '.cache/touhou-threadless-b94985982/engine'
    cache = repository / 'artifacts/webgpu-engine-f329e39c'
    manifest = json.loads((cache / 'source-manifest.json').read_text(encoding='utf-8'))
    if manifest['gpuRevision'] != 'f329e39ce8db7acaa5c9d6628a530fb769969228' or manifest['monoRevision'] != 'b94985982075d0c7d73bbced427516ce5f3e140f':
        raise RuntimeError('Unexpected engine revisions')
    if not (baseline / 'SConstruct').is_file():
        raise RuntimeError('Existing isolated Mono toolchain is unavailable')
    workspace.mkdir(parents=True, exist_ok=True)
    ownership = workspace / 'ownership.json'
    identity = {'gpuRevision': manifest['gpuRevision'], 'monoRevision': manifest['monoRevision']}
    if ownership.exists() and json.loads(ownership.read_text(encoding='utf-8')) != identity:
        raise RuntimeError('Workspace is owned by another engine attempt')
    write_text(ownership, json.dumps(identity, indent=2))
    engine = workspace / 'engine'
    copied = workspace / 'baseline-copied.json'
    if not copied.exists():
        print('ENGINE_COPY_BEGIN', baseline, engine, flush=True)
        shutil.copytree(baseline, engine, dirs_exist_ok=True)
        write_text(copied, json.dumps(identity, indent=2))
        print('ENGINE_COPY_END', flush=True)
    records = []
    conflicts = []
    for entry in manifest['files']:
        relative = pathlib.PurePosixPath(entry['path'])
        if relative.is_absolute() or '..' in relative.parts:
            raise RuntimeError('Unsafe source path')
        candidate = cache / 'candidate' / relative
        candidate_bytes = candidate.read_bytes()
        actual_blob = hashlib.sha1(b'blob ' + str(len(candidate_bytes)).encode('ascii') + b'\0' + candidate_bytes).hexdigest()
        if actual_blob != entry['candidateSha']:
            raise RuntimeError('Candidate digest mismatch: ' + str(relative))
        ours = baseline / relative
        target = engine / relative
        before_hash = digest(ours) if ours.exists() else None
        if entry['baseSha']:
            base = cache / 'base' / relative
            base_bytes = base.read_bytes()
            base_blob = hashlib.sha1(b'blob ' + str(len(base_bytes)).encode('ascii') + b'\0' + base_bytes).hexdigest()
            if base_blob != entry['baseSha']:
                raise RuntimeError('Merge-base digest mismatch: ' + str(relative))
            theirs = candidate
            mine = ours
            if str(relative) == 'platform/web/detect.py':
                mine = workspace / 'merge-inputs/mono-web-detect.py'
                text = replace_once(ours.read_text(encoding='utf-8'), '        "supported": ["mono"],\n', '')
                write_text(mine, text)
            result = subprocess.run(['git', 'merge-file', '-p', '--diff3', str(mine), str(base), str(theirs)], capture_output=True)
            merged = result.stdout.decode('utf-8')
            if result.returncode:
                conflict = cache / 'merge-conflicts' / relative
                write_text(conflict, merged)
                conflicts.append({'path': str(relative), 'exitCode': result.returncode, 'stderr': result.stderr.decode('utf-8', errors='replace')})
                continue
            if str(relative) == 'platform/web/detect.py':
                merged = replace_once(merged, '"supported": ["webgpu"]', '"supported": ["mono", "webgpu"]')
            write_text(target, merged)
        else:
            if ours.exists() and ours.read_bytes() != candidate_bytes:
                conflicts.append({'path': str(relative), 'reason': 'Added candidate file already exists in Mono base'})
                continue
            write_text(target, candidate_bytes.decode('utf-8'))
        records.append({'path': str(relative), 'baselineSha256': before_hash, 'integratedSha256': digest(target)})
    baseline_unchanged = all(record['baselineSha256'] == (digest(baseline / record['path']) if (baseline / record['path']).exists() else None) for record in records)
    supported = []
    if not conflicts:
        tree = ast.parse((engine / 'platform/web/detect.py').read_text(encoding='utf-8'))
        flags = next(node for node in tree.body if isinstance(node, ast.FunctionDef) and node.name == 'get_flags')
        supported = ast.literal_eval(flags.body[0].value)['supported']
    report = {'identity': identity, 'workspace': str(workspace), 'engine': str(engine), 'baseline': str(baseline), 'records': records, 'conflicts': conflicts, 'supported': supported, 'baselineUnchanged': baseline_unchanged, 'passed': not conflicts and baseline_unchanged and set(supported) >= {'mono', 'webgpu'}}
    write_text(cache / 'preparation.json', json.dumps(report, indent=2))
    print('ENGINE_PREPARATION', json.dumps({'files': len(records), 'conflicts': conflicts, 'baselineUnchanged': baseline_unchanged}), flush=True)
    if not report['passed']:
        raise SystemExit(2)


if __name__ == '__main__':
    main()
