import argparse
import gzip
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tarfile


ROOT = Path('/srv/touhou-survivor')
MAIN = Path('/etc/caddy/Caddyfile')
SNIPPET = Path('/etc/caddy/touhou-survivor.caddy')
IMPORT = '\timport /etc/caddy/touhou-survivor.caddy'


def sha256(filename):
    with filename.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def run(*command):
    subprocess.run(command, check=True)


def atomic_text(filename, content):
    temporary = filename.with_name(filename.name + '.next')
    temporary.write_text(content, encoding='utf-8')
    temporary.chmod(0o644)
    os.replace(temporary, filename)


def entry_html(html, release_id, transfers=None):
    prefix = '/TouhouSurvivor/releases/' + release_id + '/'
    match = re.search(r'const GODOT_CONFIG = (\{[^\n]+\});', html)
    if match is None:
        raise ValueError('Unrecognized Godot shell')
    configuration = json.loads(match.group(1))
    if configuration['executable'] != 'index' or configuration['args']:
        raise ValueError('Unexpected engine launch configuration')
    configuration['executable'] = prefix + 'index'
    configuration['fileSizes'] = {prefix + name: size for name, size in configuration['fileSizes'].items()}
    html = html[:match.start(1)] + json.dumps(configuration, separators=(',', ':')) + html[match.end(1):]
    if transfers is not None:
        marker = 'const TOUHOU_DOWNLOADS = null;'
        if marker not in html:
            raise ValueError('Download manifest placeholder missing')
        html = html.replace(marker, 'const TOUHOU_DOWNLOADS = ' + json.dumps(transfers, separators=(',', ':')) + ';', 1)
    return re.sub(r'((?:src|href)=")(?=index[.])', lambda match: match.group(1) + prefix, html)


def activate(arguments):
    if not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9._-]{1,120}', arguments.release):
        raise ValueError('Unsafe release ID')
    if sha256(arguments.archive) != arguments.sha256.lower():
        raise ValueError('Upload checksum mismatch')
    if sha256(MAIN) != arguments.config_sha256.lower():
        raise ValueError('Caddy configuration changed since inspection')
    public = ROOT / 'public'
    release = public / 'releases' / arguments.release
    backup = ROOT / 'backups' / arguments.release
    release.mkdir(parents=True, exist_ok=False)
    backup.mkdir(parents=True, exist_ok=False)
    old_main = MAIN.read_text(encoding='utf-8')
    old_snippet = SNIPPET.read_text(encoding='utf-8') if SNIPPET.exists() else None
    entry = public / 'index.html'
    old_entry = entry.read_text(encoding='utf-8') if entry.exists() else None
    shutil.copy2(MAIN, backup / 'Caddyfile')
    if old_snippet is not None:
        shutil.copy2(SNIPPET, backup / 'touhou-survivor.caddy')
    if old_entry is not None:
        shutil.copy2(entry, backup / 'index.html')
    with tarfile.open(arguments.archive, 'r:gz') as archive:
        members = {member.name.removeprefix('./'): member for member in archive.getmembers() if member.isfile()}
        metadata = json.load(archive.extractfile(members['deployment.json']))
        if metadata['releaseId'] != arguments.release:
            raise ValueError('Release metadata mismatch')
        expected = {item['name'] for item in metadata['files']} | {'deployment.json'}
        if set(members) != expected:
            raise ValueError('Unexpected archive files')
        for item in metadata['files']:
            name = item['name']
            if not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9._-]*', name):
                raise ValueError('Unsafe artifact name')
            destination = release / name
            with archive.extractfile(members[name]) as source, destination.open('wb') as output:
                shutil.copyfileobj(source, output)
            destination.chmod(0o644)
            if destination.stat().st_size != item['bytes'] or sha256(destination) != item['sha256'].lower():
                raise ValueError('Artifact checksum mismatch: ' + name)
    prefix = '/TouhouSurvivor/releases/' + arguments.release + '/'
    for filename in release.iterdir():
        if filename.suffix not in {'.wasm', '.pck', '.js', '.html', '.txt'}:
            continue
        with filename.open('rb') as source, filename.with_name(filename.name + '.gz').open('wb') as destination:
            with gzip.GzipFile(filename='', mode='wb', compresslevel=9, fileobj=destination, mtime=0) as compressor:
                shutil.copyfileobj(source, compressor)
    transfers = [{
        'url': prefix + item['name'], 'download': prefix + item['name'] + '.gz',
        'bytes': (release / (item['name'] + '.gz')).stat().st_size,
        'decodedBytes': item['bytes'], 'compressed': True,
    } for item in metadata['files'] if item['name'].endswith(('.wasm', '.pck'))]
    html = entry_html((release / 'index.html').read_text(encoding='utf-8'), arguments.release, transfers)
    candidate = old_main
    if IMPORT.strip() not in old_main:
        opening = 'allinagent.top, www.allinagent.top {'
        if old_main.count(opening) != 1:
            raise ValueError('Expected domain block not found exactly once')
        candidate = old_main.replace(opening, opening + '\n' + IMPORT, 1)
    changed = False
    try:
        changed = True
        atomic_text(SNIPPET, arguments.snippet.read_text(encoding='utf-8'))
        atomic_text(MAIN, candidate)
        run('caddy', 'validate', '--config', str(MAIN), '--adapter', 'caddyfile')
        atomic_text(entry, html)
        run('systemctl', 'reload', 'caddy')
        redirect = subprocess.check_output(['curl', '--silent', '--show-error', '--max-time', '30', '--resolve', 'allinagent.top:443:127.0.0.1', '-o', '/dev/null', '-w', '%{http_code}', 'https://allinagent.top/TouhouSurvivor'])
        if redirect != b'308':
            raise RuntimeError('Stable entry redirect failed')
        response = subprocess.check_output(['curl', '--fail', '--silent', '--show-error', '--max-time', '30', '--resolve', 'allinagent.top:443:127.0.0.1', 'https://allinagent.top/TouhouSurvivor/'])
        if prefix.encode() not in response:
            raise RuntimeError('Activated entry did not reference the expected release')
        run('systemctl', 'is-active', '--quiet', 'caddy')
    except Exception:
        if changed:
            atomic_text(MAIN, old_main)
            if old_snippet is not None:
                atomic_text(SNIPPET, old_snippet)
            if old_entry is not None:
                atomic_text(entry, old_entry)
            run('caddy', 'validate', '--config', str(MAIN), '--adapter', 'caddyfile')
            run('systemctl', 'reload', 'caddy')
        raise
    metadata['entrySha256'] = sha256(entry)
    metadata['backup'] = str(backup)
    metadata['url'] = 'https://allinagent.top/TouhouSurvivor/'
    metadata['gzipBytes'] = sum(filename.stat().st_size for filename in release.glob('*.gz'))
    atomic_text(backup / 'deployment.json', json.dumps(metadata, indent=2) + '\n')
    atomic_text(ROOT / 'active.json', json.dumps(metadata, indent=2) + '\n')
    print('WEB_DEPLOYMENT_ACTIVE ' + arguments.release, flush=True)
    print(json.dumps({key: metadata[key] for key in ['url', 'sourceCommit', 'entrySha256', 'gzipBytes', 'backup']}, indent=2))


def main():
    import fcntl

    parser = argparse.ArgumentParser()
    parser.add_argument('--archive', type=Path, required=True)
    parser.add_argument('--snippet', type=Path, required=True)
    parser.add_argument('--release', required=True)
    parser.add_argument('--sha256', required=True)
    parser.add_argument('--config-sha256', required=True)
    arguments = parser.parse_args()
    ROOT.mkdir(parents=True, exist_ok=True)
    with (ROOT / 'deploy.lock').open('a', encoding='utf-8') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        activate(arguments)


if __name__ == '__main__':
    main()
