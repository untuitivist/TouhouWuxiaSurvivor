const test = require('node:test');
const assert = require('node:assert/strict');
const { startServer } = require('./server.cjs');

test('lab serves only its allowlisted files without shared-memory headers', async () => {
    const host = await startServer();
    try {
        const page = await fetch(host.origin);
        assert.equal(page.status, 200);
        assert.equal(page.headers.get('cross-origin-opener-policy'), null);
        assert.equal(page.headers.get('cross-origin-embedder-policy'), null);
        assert.match(await page.text(), /1,000/);
        for (const suffix of ['/.NOTE.md', '/.git/config', '/%2e%2e/.NOTE.md', '/release/TouhouWuxiaSurvivor_alpha-0.1.7.exe']) assert.equal((await fetch(host.origin + suffix)).status, 404);
        assert.equal((await fetch(host.origin, { method: 'POST' })).status, 405);
        const image = await fetch(host.origin + '/sprite.png');
        assert.equal(image.headers.get('content-type'), 'image/png');
        const bytes = Buffer.from(await image.arrayBuffer());
        assert.equal(bytes.readUInt32BE(16), 192);
        assert.equal(bytes.readUInt32BE(20), 48);
        const head = await fetch(host.origin + '/sprite.png', { method: 'HEAD' });
        assert.equal(Number(head.headers.get('content-length')), bytes.length);
        assert.equal((await head.arrayBuffer()).byteLength, 0);
        assert.equal(new URL(host.origin).hostname, '127.0.0.1');
    } finally { await host.close(); }
});
