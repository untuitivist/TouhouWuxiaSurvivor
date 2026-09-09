const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');

const root = path.resolve(__dirname, '../..');
const texturePath = 'assets/internal_original/base/actors/wild_fairy.png';
const files = new Map([
    ['/', ['tools/webgpu/index.html', 'text/html; charset=utf-8']],
    ...['scene.mjs', 'renderers.mjs', 'lab.mjs'].map(name => ['/' + name, ['tools/webgpu/' + name, 'text/javascript; charset=utf-8']]),
    ['/sprite.png', [texturePath, 'image/png']]
]);

async function startServer(port = 0) {
    const server = http.createServer((request, response) => {
        response.setHeader('Cache-Control', 'no-store');
        response.setHeader('X-Content-Type-Options', 'nosniff');
        response.setHeader('Content-Security-Policy', "default-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self'; connect-src 'self'");
        if (!['GET', 'HEAD'].includes(request.method)) { response.writeHead(405).end(); return; }
        let pathname;
        try { pathname = new URL(request.url, 'http://localhost').pathname; }
        catch { response.writeHead(400).end(); return; }
        if (pathname === '/favicon.ico') { response.writeHead(204).end(); return; }
        const file = files.get(pathname);
        if (!file) { response.writeHead(404).end(); return; }
        try {
            const bytes = fs.readFileSync(path.join(root, file[0]));
            response.writeHead(200, { 'Content-Type': file[1], 'Content-Length': bytes.length });
            response.end(request.method === 'HEAD' ? undefined : bytes);
        } catch { response.writeHead(500).end(); }
    });
    await new Promise((resolve, reject) => { server.once('error', reject); server.listen(port || crypto.randomInt(49152, 64000), '127.0.0.1', resolve); });
    return { server, origin: 'http://127.0.0.1:' + server.address().port, close: async () => { server.closeAllConnections(); await new Promise(resolve => server.close(resolve)); } };
}

module.exports = { startServer, texturePath };
if (require.main === module) startServer(8769).then(host => console.log('LOCAL_RENDER_LAB ' + host.origin + ' (not the game; Ctrl+C to stop)')).catch(error => { console.error(error); process.exitCode = 1; });
