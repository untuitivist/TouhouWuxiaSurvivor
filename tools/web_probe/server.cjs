const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');

function startProbeServer(root, port = 0) {
    const site = fs.realpathSync(root);
    const types = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.mjs': 'text/javascript', '.wasm': 'application/wasm', '.pck': 'application/octet-stream', '.png': 'image/png' };
    const requests = [];
    const server = http.createServer((request, response) => {
        response.setHeader('Cache-Control', 'no-store');
        response.setHeader('Cross-Origin-Opener-Policy', 'same-origin');
        response.setHeader('Cross-Origin-Embedder-Policy', 'require-corp');
        response.setHeader('X-Content-Type-Options', 'nosniff');
        let pathname;
        try { pathname = decodeURIComponent(new URL(request.url, 'http://localhost').pathname); }
        catch { response.writeHead(400).end(); return; }
        if (pathname === '/TouhouSurvivor') {
            response.writeHead(308, { Location: '/TouhouSurvivor/' }).end();
            return;
        }
        if (!pathname.startsWith('/TouhouSurvivor/') || !['GET', 'HEAD'].includes(request.method)) {
            response.writeHead(404).end();
            return;
        }
        const relative = pathname.endsWith('/') ? pathname + 'index.html' : pathname;
        const filename = path.resolve(site, '.' + relative);
        if (!filename.startsWith(site + path.sep)) { response.writeHead(403).end(); return; }
        try {
            const real = fs.realpathSync(filename);
            if (!real.startsWith(site + path.sep)) { response.writeHead(403).end(); return; }
            const stats = fs.statSync(real);
            if (!stats.isFile()) { response.writeHead(404).end(); return; }
            requests.push({ path: pathname, status: 200, bytes: stats.size });
            response.writeHead(200, { 'Content-Type': types[path.extname(real)] || 'application/octet-stream', 'Content-Length': stats.size });
            if (request.method === 'HEAD') { response.end(); return; }
            const stream = fs.createReadStream(real);
            response.on('close', () => stream.destroy());
            stream.on('error', () => response.destroy());
            stream.pipe(response);
        } catch {
            requests.push({ path: pathname, status: 404 });
            response.writeHead(404).end();
        }
    });
    return new Promise((resolve, reject) => {
        server.once('error', reject);
        server.listen(port, '127.0.0.1', () => resolve({ server, requests, origin: `http://127.0.0.1:${server.address().port}` }));
    });
}

module.exports = { startProbeServer };

if (require.main === module) {
    const root = path.resolve(__dirname, '../../artifacts/web-probe-20260906/site');
    startProbeServer(root, 8765).then(({ origin }) => console.log(`Local probe only: ${origin}/TouhouSurvivor/`)).catch(error => {
        console.error(error);
        process.exitCode = 1;
    });
}
