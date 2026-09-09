const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');

const GPU_REVISION = 'f329e39ce8db7acaa5c9d6628a530fb769969228';
const BASE_REVISION = '001aa128b1cd80dc4e47e823c360bccf45ed6bad';
const MONO_REVISION = 'b94985982075d0c7d73bbced427516ce5f3e140f';
const root = path.resolve(__dirname, '../..');
const cache = path.resolve(root, 'artifacts/webgpu-engine-f329e39c');
const sourcePattern = /^(SConstruct$|core\/|drivers\/|main\/|modules\/|platform\/web\/|scene\/|servers\/|thirdparty\/|misc\/dist\/html\/)/;
const blobHash = bytes => crypto.createHash('sha1').update('blob ' + bytes.length + '\0').update(bytes).digest('hex');

async function request(url) {
    for (let attempt = 0; attempt < 3; attempt++) {
        try {
            const response = await fetch(url, { headers: { 'User-Agent': 'Touhou-WebGPU-FullValidation' }, signal: AbortSignal.timeout(45000) });
            if (!response.ok) throw new Error('HTTP ' + response.status + ': ' + url);
            return Buffer.from(await response.arrayBuffer());
        } catch (error) {
            if (attempt === 2) throw error;
            console.log('SOURCE_RETRY', attempt + 1, String(error));
            await new Promise(resolve => setTimeout(resolve, 1000 * (attempt + 1)));
        }
    }
}

async function readTree(repository, revision, label) {
    const filename = path.join(cache, label + '-tree.json');
    let bytes;
    try { bytes = await fs.readFile(filename); }
    catch (error) { if (error.code !== 'ENOENT') throw error; bytes = await request('https://api.github.com/repos/' + repository + '/git/trees/' + revision + '?recursive=1'); }
    const tree = JSON.parse(bytes.toString('utf8'));
    if (tree.truncated || !Array.isArray(tree.tree)) throw new Error('Source tree is incomplete: ' + label);
    if (tree.tree.some(entry => entry.path.split('/').includes('..') || path.isAbsolute(entry.path))) throw new Error('Unsafe source path');
    await fs.writeFile(filename, JSON.stringify(tree, null, 2), 'utf8');
    return new Map(tree.tree.filter(entry => entry.type === 'blob').map(entry => [entry.path, entry]));
}

async function main() {
    await fs.mkdir(cache, { recursive: true });
    await fs.writeFile(path.join(cache, '.gdignore'), '', 'utf8');
    const toolchain = JSON.parse(await fs.readFile(path.join(root, 'tools/threadless/toolchain.json'), 'utf8'));
    if (toolchain.engineRevision !== MONO_REVISION) throw new Error('Maintained Mono toolchain has changed; re-audit first');
    const [candidate, base] = await Promise.all([readTree('dwalter/godotwebgpu', GPU_REVISION, 'candidate'), readTree('godotengine/godot', BASE_REVISION, 'base')]);
    const files = [...candidate.values()].filter(entry => sourcePattern.test(entry.path) && entry.sha !== base.get(entry.path)?.sha).map(entry => ({ path: entry.path, candidateSha: entry.sha, candidateBytes: entry.size, baseSha: base.get(entry.path)?.sha ?? null, baseBytes: base.get(entry.path)?.size ?? 0 }));
    const removed = [...base.keys()].filter(relative => sourcePattern.test(relative) && !candidate.has(relative));
    if (removed.length) throw new Error('Candidate requires file removals; manual approval needed: ' + removed.join(', '));
    const jobs = files.flatMap(file => [{ label: 'candidate', repository: 'dwalter/godotwebgpu', revision: GPU_REVISION, relative: file.path, sha: file.candidateSha, size: file.candidateBytes }, ...(file.baseSha ? [{ label: 'base', repository: 'godotengine/godot', revision: BASE_REVISION, relative: file.path, sha: file.baseSha, size: file.baseBytes }] : [])]);
    const totalBytes = jobs.reduce((sum, job) => sum + job.size, 0);
    let position = 0;
    let completed = 0;
    let completedBytes = 0;
    let downloadedBytes = 0;
    const started = Date.now();
    console.log('SOURCE_SCOPE', JSON.stringify({ files: files.length, downloads: jobs.length, bytes: totalBytes, cache, removed }));
    const timer = setInterval(() => console.log('SOURCE_PROGRESS', JSON.stringify({ completed, total: jobs.length, completedBytes, totalBytes, downloadedBytes, bytesPerSecond: downloadedBytes / ((Date.now() - started) / 1000) })), 5000);
    try {
        await Promise.all(Array.from({ length: 8 }, async () => {
            while (position < jobs.length) {
                const job = jobs[position++];
                const destination = path.join(cache, job.label, job.relative);
                let bytes;
                try { bytes = await fs.readFile(destination); }
                catch (error) { if (error.code !== 'ENOENT') throw error; }
                if (bytes && blobHash(bytes) !== job.sha) throw new Error('Cached source digest mismatch: ' + destination);
                if (!bytes) {
                    bytes = await request('https://raw.githubusercontent.com/' + job.repository + '/' + job.revision + '/' + job.relative);
                    if (bytes.length !== job.size || blobHash(bytes) !== job.sha) throw new Error('Downloaded source digest mismatch: ' + job.relative);
                    await fs.mkdir(path.dirname(destination), { recursive: true });
                    await fs.writeFile(destination, bytes);
                    downloadedBytes += bytes.length;
                }
                completed++;
                completedBytes += bytes.length;
            }
        }));
    } finally { clearInterval(timer); }
    const manifest = { gpuRevision: GPU_REVISION, upstreamBaseRevision: BASE_REVISION, monoRevision: MONO_REVISION, acquiredAt: new Date().toISOString(), files, removed, totalBytes, completedBytes, downloadedBytes, sourceScope: 'Engine-only backport onto isolated existing Mono 4.6.1; not a game fork or published renderer' };
    await fs.writeFile(path.join(cache, 'source-manifest.json'), JSON.stringify(manifest, null, 2), 'utf8');
    console.log('ENGINE_SOURCES_PASS', JSON.stringify({ cache, files: files.length, completedBytes, totalBytes }));
}

main().catch(error => { console.error(error); process.exitCode = 1; });
