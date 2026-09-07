const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const http = require('node:http');
const { gzipSync } = require('node:zlib');
const { execFileSync } = require('node:child_process');

async function startDeploymentFixture(build, { isolation = true, entryArguments = null } = {}) {
    const root = path.resolve(__dirname, '../..');
    const site = path.join(build.site, 'TouhouSurvivor');
    const release = 'verification-fixture';
    const prefix = '/TouhouSurvivor/releases/' + release + '/';
    const payloads = new Map();
    const transfers = [];
    for (const entry of await fs.readdir(site, { withFileTypes: true })) {
        if (!entry.isFile() || !/^[A-Za-z0-9][A-Za-z0-9._-]*$/.test(entry.name)) continue;
        const data = await fs.readFile(path.join(site, entry.name));
        payloads.set(prefix + entry.name, data);
        if (/\.(wasm|pck|js|html|txt)$/.test(entry.name)) payloads.set(prefix + entry.name + '.gz', gzipSync(data, { level: 9 }));
        if (/\.(wasm|pck)$/.test(entry.name)) transfers.push({ url: prefix + entry.name, download: prefix + entry.name + '.gz', bytes: payloads.get(prefix + entry.name + '.gz').length, decodedBytes: data.length, compressed: true });
    }
    assert.equal(transfers.length, 2);
    const python = 'C:/Users/untuitivist/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe';
    const html = execFileSync(python, ['-B', '-c', 'import sys,json; from pathlib import Path; sys.path.insert(0,"tools/platform"); from activate_deployment import entry_html; sys.stdout.buffer.write(entry_html(Path(sys.argv[1]).read_text(encoding="utf-8"),sys.argv[2],json.loads(sys.argv[3])).encode("utf-8"))', path.join(site, 'index.html'), release, JSON.stringify(transfers)], { cwd: root });
    const requests = [];
    const types = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.wasm': 'application/wasm', '.pck': 'application/octet-stream', '.png': 'image/png', '.gz': 'application/gzip', '.txt': 'text/plain; charset=utf-8' };
    const server = http.createServer((request, response) => {
        const pathname = new URL(request.url, 'http://localhost').pathname;
        if (isolation) {
            response.setHeader('Cross-Origin-Opener-Policy', 'same-origin');
            response.setHeader('Cross-Origin-Embedder-Policy', 'require-corp');
        }
        response.setHeader('X-Content-Type-Options', 'nosniff');
        response.setHeader('Cache-Control', pathname.startsWith(prefix) ? 'public, max-age=31536000, immutable' : 'no-cache');
        if (!['GET', 'HEAD'].includes(request.method)) { response.writeHead(404).end(); return; }
        if (pathname === '/TouhouSurvivor') { response.writeHead(308, { Location: '/TouhouSurvivor/' }).end(); return; }
        let payload = pathname === '/TouhouSurvivor/' ? html : payloads.get(pathname);
        if (pathname === '/TouhouSurvivor/' && entryArguments) {
            const args = typeof entryArguments === 'function' ? entryArguments() : entryArguments;
            payload = Buffer.from(html.toString('utf8').replace('"args":[]', `"args":${JSON.stringify(args)}`), 'utf8');
        }
        if (!payload) { response.writeHead(404).end(); return; }
        const acceptsGzip = /(?:^|,)\s*gzip\s*(?:,|$)/i.test(request.headers['accept-encoding'] || '');
        if (!pathname.endsWith('.gz') && payloads.has(pathname + '.gz')) {
            response.setHeader('Vary', 'Accept-Encoding');
            if (acceptsGzip) { payload = payloads.get(pathname + '.gz'); response.setHeader('Content-Encoding', 'gzip'); }
        }
        requests.push({ path: pathname, method: request.method, bytes: payload.length });
        response.writeHead(200, { 'Content-Type': pathname === '/TouhouSurvivor/' ? types['.html'] : types[path.extname(pathname)] || 'application/octet-stream', 'Content-Length': payload.length });
        response.end(request.method === 'HEAD' ? undefined : payload);
    });
    await new Promise((resolve, reject) => { server.once('error', reject); server.listen(0, '127.0.0.1', resolve); });
    return { server, requests, transfers, transferMode: 'deployment-gzip', wasmPath: prefix + 'index.wasm', origin: 'http://127.0.0.1:' + server.address().port };
}

module.exports = { startDeploymentFixture };
