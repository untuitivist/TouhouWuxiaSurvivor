const fs = require('node:fs/promises');
const path = require('node:path');
const assert = require('node:assert/strict');
const { chromium } = require('playwright');
const { startProbeServer } = require('../web_probe/server.cjs');

async function main() {
    const root = path.resolve(__dirname, '../..');
    const build = JSON.parse(await fs.readFile(path.join(root, 'artifacts/web-compatible-latest.json'), 'utf8'));
    const host = await startProbeServer(build.site, 0, { isolation: false, entryArguments: ['--', '--web-validation'] });
    const browser = await chromium.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true, args: ['--enable-unsafe-swiftshader'] });
    const context = await browser.newContext({ viewport: { width: 844, height: 390 }, hasTouch: true, isMobile: true });
    const page = await context.newPage();
    if (process.argv.includes('--trace-runtime')) {
        await page.route('**/index.js', async route => {
            const response = await route.fetch();
            const body = await response.text();
            const marker = 'checkStackCookie();if(e instanceof WebAssembly.RuntimeError)';
            assert.ok(body.includes(marker));
            const traced = body.replace(marker, 'console.error("RUNTIME_CAUSE", e?.stack || e);' + marker)
                .replace('var exitJS=(status,implicit)=>{', 'var exitJS=(status,implicit)=>{if(status)console.error("EXIT_ORIGIN", status, new Error().stack);')
                .replace('function _mono_wasm_trace_logger(){return{runtime_idx:16}}', 'function _mono_wasm_trace_logger(domain,level,message,fatal){console.error("MONO_TRACE",UTF8ToString(domain),UTF8ToString(level),UTF8ToString(message),fatal)}');
            await route.fulfill({ response, body: traced });
        });
    }
    const report = { browser: browser.version(), physicalDeviceTested: false, events: [], failedRequests: [] };
    page.on('console', message => { if (report.events.length < 400) report.events.push({ type: message.type(), text: message.text() }); });
    page.on('pageerror', error => report.events.push({ type: 'pageerror', text: String(error) }));
    page.on('requestfailed', request => report.failedRequests.push({ url: request.url(), error: request.failure()?.errorText }));
    assert.ok((await fs.readFile(path.join(build.site, 'TouhouSurvivor/index.html'), 'utf8')).includes('TouhouLoading.create(GODOT_CONFIG, TOUHOU_DOWNLOADS, false)'));
    try {
        await page.goto(host.origin + '/TouhouSurvivor/', { waitUntil: 'domcontentloaded' });
        await page.waitForFunction(() => window.__touhouProbe && document.body.dataset.gameReady === 'true', {}, { timeout: process.argv.includes('--trace-runtime') ? 12000 : 90000 });
        await page.waitForTimeout(1500);
        report.state = await page.evaluate(() => ({ isolated: crossOriginIsolated, sharedArrayBuffer: typeof SharedArrayBuffer, game: window.__touhouProbe }));
        assert.equal(report.state.isolated, false);
        assert.equal(report.state.sharedArrayBuffer, 'undefined');
        assert.equal(report.state.game.Screen, 'title');
        assert.equal(report.state.game.HasChineseGlyphs, true);
        assert.equal(report.state.game.Warning, '');
        assert.deepEqual(report.events.filter(event => ['error', 'pageerror'].includes(event.type)), []);
        assert.deepEqual(report.failedRequests, []);
        report.passed = true;
        console.log('THREADLESS_STARTUP_PASS', JSON.stringify(report.state));
    } catch (error) {
        report.error = String(error);
        report.page = await page.evaluate(() => ({ isolated: crossOriginIsolated, sharedArrayBuffer: typeof SharedArrayBuffer, text: document.body.innerText, game: window.__touhouProbe }));
        throw error;
    } finally {
        await page.screenshot({ path: path.join(build.build, 'threadless-startup.png') });
        await browser.close();
        host.server.closeAllConnections();
        await new Promise(resolve => host.server.close(resolve));
        await fs.writeFile(path.join(build.build, 'threadless-startup.json'), JSON.stringify(report, null, 2), 'utf8');
    }
}

main().catch(error => { console.error(error); process.exitCode = 1; });
