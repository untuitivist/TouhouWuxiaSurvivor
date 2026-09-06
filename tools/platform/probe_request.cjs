const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { createHash } = require('node:crypto');
const { chromium } = require('playwright');
const { startProbeServer } = require('../web_probe/server.cjs');

async function main() {
    const root = path.resolve(__dirname, '../..');
    const build = JSON.parse(await fs.readFile(path.join(root, 'artifacts/web-latest.json'), 'utf8'));
    const host = await startProbeServer(build.site);
    const browser = await chromium.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true, args: ['--enable-unsafe-swiftshader'] });
    const reports = [];
    const pack = await fs.readFile(path.join(build.site, 'TouhouSurvivor/index.pck'));
    const expected = { bytes: pack.length, sha256: createHash('sha256').update(pack).digest('hex') };
    try {
        const attempts = Number(process.argv.find(argument => argument.startsWith('--attempts='))?.split('=')[1] ?? 8);
        assert.ok(Number.isSafeInteger(attempts) && attempts > 0 && attempts <= 100);
        for (let attempt = 0; attempt < attempts; attempt++) {
            const context = await browser.newContext({ viewport: { width: 844, height: 390 }, hasTouch: true, isMobile: true });
            const page = await context.newPage();
            if (process.argv.includes('--download-only')) {
                const consume = process.argv.includes('--stream-response') ? 'const reader = response.body.getReader(); return new Response(new ReadableStream({ async start(controller) { while (true) { const result = await reader.read(); if (result.done) break; controller.enqueue(result.value); } controller.close(); } })).arrayBuffer();' : 'return response.arrayBuffer();';
                await page.route('**/TouhouSurvivor/', route => route.fulfill({ contentType: 'text/html', body: '<!doctype html><body><script>fetch("index.pck").then(response => { ' + consume + ' }).then(async buffer => { window.packBytes = buffer.byteLength; window.packHash = Array.from(new Uint8Array(await crypto.subtle.digest("SHA-256", buffer)), value => value.toString(16).padStart(2, "0")).join(""); document.body.dataset.gameReady = "true"; });</script>' }));
            }
            const network = await context.newCDPSession(page);
            const requests = new Map();
            const failed = [];
            await network.send('Network.enable');
            network.on('Network.requestWillBeSent', event => { if (event.request.url.endsWith('.pck')) requests.set(event.requestId, { ...event, chunks: [] }); });
            network.on('Network.responseReceived', event => { if (requests.has(event.requestId)) requests.get(event.requestId).response = event.response; });
            network.on('Network.dataReceived', event => { if (requests.has(event.requestId)) requests.get(event.requestId).chunks.push(event); });
            network.on('Network.loadingFailed', event => { if (requests.has(event.requestId)) { requests.get(event.requestId).failed = event; failed.push(event); } });
            network.on('Network.loadingFinished', event => { if (requests.has(event.requestId)) requests.get(event.requestId).finished = event; });
            await page.addInitScript(() => {
                window.packCalls = [];
                window.packAborts = [];
                const fetchResource = window.fetch;
                window.fetch = function(input, options) {
                    const isPack = String(input).includes('.pck');
                    if (isPack) {
                        window.packCalls.push({ input: String(input), options, stack: new Error().stack });
                        options?.signal?.addEventListener('abort', () => window.packAborts.push({ time: performance.now(), reason: String(options.signal.reason), stack: new Error().stack }));
                    }
                    return fetchResource.call(window, input, options);
                };
            });
            await page.goto(host.origin + '/TouhouSurvivor/', { waitUntil: 'domcontentloaded' });
            await page.waitForFunction(() => document.body.dataset.gameReady === 'true', {}, { timeout: 90000 });
            await page.waitForTimeout(1000);
            const state = await page.evaluate(() => ({ calls: window.packCalls, aborts: window.packAborts, bytes: window.packBytes, sha256: window.packHash, resources: performance.getEntriesByType('resource').filter(entry => entry.name.includes('.pck')).map(entry => entry.toJSON()) }));
            reports.push({ attempt, expected, requests: [...requests.values()], state });
            console.log('REQUEST_PROBE', attempt, failed.length);
            await context.close();
            if (process.argv.includes('--download-only')) {
                assert.equal(state.bytes, expected.bytes);
                assert.equal(state.sha256, expected.sha256);
                console.log('PACK_INTEGRITY_PASS', expected.sha256);
            }
            assert.deepEqual(failed, [], 'Pack requests must finish without cancellation');
            assert.ok(requests.size > 0, 'Expected a pack request');
            assert.ok([...requests.values()].every(request => request.finished), 'Expected loadingFinished for every pack request');
        }
    } finally {
        await browser.close();
        host.server.closeAllConnections();
        await new Promise(resolve => host.server.close(resolve));
        await fs.writeFile(path.join(build.build, 'request-probe.json'), JSON.stringify(reports, null, 2), 'utf8');
    }
}

main().catch(error => { console.error(error); process.exitCode = 1; });
