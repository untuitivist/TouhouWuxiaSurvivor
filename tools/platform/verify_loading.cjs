const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const { gzipSync } = require('node:zlib');
const { execFileSync } = require('node:child_process');
const { once } = require('node:events');
const { setTimeout: delay } = require('node:timers/promises');
const { chromium } = require('playwright');

const root = path.resolve(__dirname, '../..');
const compatible = process.argv.includes('--compatible');
const build = JSON.parse(fs.readFileSync(path.join(root, compatible ? 'artifacts/web-compatible-latest.json' : 'artifacts/web-latest.json'), 'utf8'));
const site = path.join(build.site, 'TouhouSurvivor');
const output = path.join(build.build, 'loading-verification', new Date().toISOString().replace(/[:.]/g, '-'));
fs.mkdirSync(output, { recursive: true });
const runtime = 'C:/Users/untuitivist/.cache/codex-runtimes/codex-primary-runtime/dependencies';
const prefix = '/TouhouSurvivor/releases/loading-fixture/';
const payloads = new Map();
const transfers = ['index.wasm', 'index.pck'].map(name => {
    const source = fs.readFileSync(path.join(site, name));
    const compressed = gzipSync(source, { level: 9 });
    payloads.set(prefix + name + '.gz', compressed);
    return { url: prefix + name, download: prefix + name + '.gz', bytes: compressed.length, decodedBytes: source.length, compressed: true };
});
const total = transfers.reduce((sum, file) => sum + file.bytes, 0);
const html = execFileSync(path.join(runtime, 'python/python.exe'), ['-B', '-c',
    'import sys,json; from pathlib import Path; sys.path.insert(0,"tools/platform"); from activate_deployment import entry_html; sys.stdout.buffer.write(entry_html(Path(sys.argv[1]).read_text(encoding="utf-8"),"loading-fixture",json.loads(sys.argv[2])).encode("utf-8"))',
    path.join(site, 'index.html'), JSON.stringify(transfers)], { cwd: root });
let mode = 'normal';
const requests = [];
const server = http.createServer(async (request, response) => {
    const pathname = new URL(request.url, 'http://localhost').pathname;
    const currentMode = mode;
    requests.push({ mode: currentMode, pathname });
    if (!compatible) {
        response.setHeader('Cross-Origin-Opener-Policy', 'same-origin');
        response.setHeader('Cross-Origin-Embedder-Policy', 'require-corp');
    }
    response.setHeader('Cache-Control', 'no-store');
    try {
        if (pathname === '/TouhouSurvivor/') {
            response.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' }).end(html);
            return;
        }
        if (payloads.has(pathname)) {
            if (currentMode === 'resource-error') { response.writeHead(503).end(); return; }
            const payload = payloads.get(pathname);
            response.writeHead(200, { 'Content-Type': 'application/gzip', 'Content-Length': payload.length, 'Cache-Control': 'public, max-age=31536000, immutable' });
            for (let offset = 0; offset < payload.length; offset += 262144) {
                if (response.destroyed) return;
                if (!response.write(payload.subarray(offset, offset + 262144))) await once(response, 'drain');
                if (currentMode === 'stall' && offset === 0) await delay(10000);
                if (currentMode === 'disconnect' && offset >= 524288) { response.destroy(); return; }
                await delay(50);
            }
            response.end();
            return;
        }
        const name = pathname.startsWith(prefix) ? pathname.slice(prefix.length) : '';
        if (!/^[A-Za-z0-9._-]+$/.test(name)) { response.writeHead(404).end(); return; }
        if ((currentMode === 'loader-error' && name === 'index.loader.js') || (currentMode === 'engine-error' && name === 'index.js')) {
            response.writeHead(503).end(); return;
        }
        const types = { '.js': 'text/javascript', '.wasm': 'application/wasm', '.png': 'image/png' };
        const payload = fs.readFileSync(path.join(site, name));
        response.writeHead(200, { 'Content-Type': types[path.extname(name)] || 'application/octet-stream', 'Content-Length': payload.length }).end(payload);
    } catch (error) {
        if (!response.destroyed) response.destroy(error);
    }
});

async function main() {
    server.listen(0, '127.0.0.1');
    await once(server, 'listening');
    const url = `http://127.0.0.1:${server.address().port}/TouhouSurvivor/`;
    let context;
    const results = [];
    try {
        for (const scenario of ['normal', 'stall', 'disconnect', 'resource-error', 'loader-error', 'engine-error']) {
            mode = scenario;
            context = await chromium.launchPersistentContext(path.join(output, 'profile-' + scenario), {
                executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true,
                args: ['--disk-cache-size=268435456'],
                viewport: scenario === 'stall' ? { width: 390, height: 844 } : { width: 1280, height: 720 }, hasTouch: scenario === 'stall'
            });
            const page = await context.newPage();
            const errors = [];
            let interruptionErrors = [];
            page.on('pageerror', error => errors.push(error.message));
            await page.addInitScript(() => {
                window.loadingStates = [];
                window.originalLoadingFetch = window.fetch;
                new MutationObserver(records => {
                    for (const record of records) {
                        if (record.target.id === 'loading') window.loadingStates.push({ ...record.target.dataset });
                    }
                }).observe(document, { subtree: true, attributes: true, attributeFilter: ['data-stage'] });
                const timer = setInterval(() => {
                    const loading = document.getElementById('loading');
                    if (loading) window.loadingStates.push({ ...loading.dataset });
                    else clearInterval(timer);
                }, 100);
            });
            await page.goto(url, { waitUntil: 'domcontentloaded' });
            if (scenario === 'normal') {
                await page.waitForFunction(() => Number(document.getElementById('loading')?.dataset.speed) > 0);
                assert.equal(await page.locator('#loading').getAttribute('data-total'), String(total));
                assert.match(await page.locator('#speed').innerText(), /[KM]?B\/s/);
                await page.screenshot({ path: path.join(output, 'desktop-downloading.png') });
                await page.setViewportSize({ width: 844, height: 390 });
                assert.equal(await page.locator('#loading').evaluate(element => element.scrollWidth <= element.clientWidth), true);
                await page.screenshot({ path: path.join(output, 'landscape-downloading.png') });
            }
            if (scenario === 'stall') {
                await page.waitForFunction(() => document.getElementById('loading')?.dataset.stage === 'waiting', { }, { timeout: 20000 });
                const loaded = await page.locator('#loading').getAttribute('data-loaded');
                assert.equal(await page.locator('#speed').innerText(), '0 B/s');
                assert.equal(await page.locator('#rotate').isVisible(), false);
                assert.equal(await page.locator('#loading').evaluate(element => element.scrollWidth <= element.clientWidth), true);
                await page.screenshot({ path: path.join(output, 'portrait-waiting.png') });
                await delay(500);
                assert.equal(await page.locator('#loading').getAttribute('data-loaded'), loaded);
            }
            if (['disconnect', 'resource-error', 'loader-error', 'engine-error'].includes(scenario)) {
                await page.locator('#retry').waitFor({ state: 'visible' });
                const loaded = await page.locator('#loading').getAttribute('data-loaded');
                await delay(scenario === 'resource-error' ? 3500 : 500);
                assert.equal(await page.locator('#loading').getAttribute('data-loaded'), loaded);
                assert.equal(await page.evaluate(() => window.fetch === window.originalLoadingFetch), !['disconnect', 'resource-error'].includes(scenario));
                assert.equal(requests.some(request => request.mode === scenario && /\.(wasm|pck)$/.test(request.pathname)), false);
                await page.screenshot({ path: path.join(output, scenario + '.png') });
                interruptionErrors = errors.splice(0);
                if (scenario === 'disconnect') {
                    assert.ok(interruptionErrors.every(message => /^(BodyStreamBuffer was aborted|network error)$/.test(message)), JSON.stringify(interruptionErrors));
                } else if (scenario === 'resource-error') {
                    assert.ok(interruptionErrors.every(message => message === '下载已停止，请重试。'), JSON.stringify(interruptionErrors));
                } else assert.deepEqual(interruptionErrors, []);
                mode = 'normal';
                await page.locator('#retry').click();
            }
            await page.waitForFunction(() => document.body.dataset.gameReady === 'true', {}, { timeout: 120000 });
            assert.equal(await page.evaluate(() => window.fetch === window.originalLoadingFetch), true);
            assert.deepEqual(errors, []);
            const states = await page.evaluate(() => window.loadingStates);
            assert.ok(states.some(state => state.stage === 'initializing' && Number(state.loaded) === total), JSON.stringify(states.slice(-5)));
            if (scenario === 'stall') assert.equal(await page.locator('#rotate').isVisible(), true);
            if (scenario === 'normal') {
                const before = requests.filter(request => payloads.has(request.pathname)).length;
                await page.goto('about:blank');
                await page.goto(url, { waitUntil: 'domcontentloaded' });
                await page.waitForFunction(() => document.body.dataset.gameReady === 'true', {}, { timeout: 120000 });
                assert.equal(requests.filter(request => payloads.has(request.pathname)).length, before);
            }
            results.push({ scenario, passed: true, errors, interruptionErrors, states });
            console.log('LOADING_SCENARIO_PASS ' + scenario);
            await context.close();
            context = null;
        }
        fs.writeFileSync(path.join(output, 'report.json'), JSON.stringify({ total, transfers, results, requests }, null, 2), 'utf8');
        console.log('LOADING_BROWSER_PASS ' + output);
    } finally {
        await context?.close();
        server.closeAllConnections();
        await new Promise(resolve => server.close(resolve));
    }
}

main().catch(error => { console.error(error); process.exitCode = 1; server.closeAllConnections(); server.close(); });
