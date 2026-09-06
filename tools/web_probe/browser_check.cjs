const fs = require('node:fs/promises');
const path = require('node:path');
const assert = require('node:assert/strict');
const { startProbeServer } = require('./server.cjs');

async function runBrowserProbe(playwright, repository) {
    const root = path.resolve(repository, 'artifacts/web-probe-20260906');
    const output = path.join(root, 'browser-check');
    await fs.mkdir(output, { recursive: true });
    const baseline = await startProbeServer(path.join(root, 'site'));
    const compatibility = await startProbeServer(path.join(root, 'compat-4.6.1/site'));
    const report = { source: (await fs.readFile(path.join(root, 'source-revision.txt'), 'utf8')).trim(), checks: [], physicalMobileTested: false, overall: 'not-deployable' };
    let browser;
    async function capture(name, origin, args = [], mobile = false, actions) {
        const context = await browser.newContext({ viewport: mobile ? { width: 844, height: 390 } : { width: 1280, height: 720 }, hasTouch: mobile, isMobile: mobile });
        const page = await context.newPage();
        const entry = { name, browser: browser.version(), mobileEmulation: mobile, events: [], failedRequests: [], responses: [] };
        page.on('console', message => entry.events.push({ type: message.type(), text: message.text() }));
        page.on('pageerror', error => entry.events.push({ type: 'pageerror', text: String(error) }));
        page.on('requestfailed', request => entry.failedRequests.push({ url: request.url(), error: request.failure()?.errorText }));
        page.on('response', response => {
            if (response.status() >= 400) entry.responses.push({ url: response.url(), status: response.status() });
        });
        try {
            if (args.length) {
                await page.route('**/TouhouSurvivor/', async route => {
                    const response = await route.fetch();
                    const html = (await response.text()).replace('"args":[]', `"args":${JSON.stringify(['--', ...args])}`);
                    await route.fulfill({ response, body: html });
                });
            }
            await page.goto(origin + '/TouhouSurvivor/', { waitUntil: 'domcontentloaded' });
            await page.waitForFunction(() => !document.querySelector('#status'), undefined, { timeout: 30000 });
            await page.waitForTimeout(args.length ? 6000 : 2500);
            entry.environment = await page.evaluate(() => ({ isolated: crossOriginIsolated, sharedArrayBuffer: typeof SharedArrayBuffer, viewport: [innerWidth, innerHeight], canvas: !!document.querySelector('canvas') }));
            if (actions) await actions(page, entry);
            await page.screenshot({ path: path.join(output, name + '.png') });
            entry.screenshot = name + '.png';
        } catch (error) {
            entry.automationError = String(error);
            await page.screenshot({ path: path.join(output, name + '-failure.png'), timeout: 5000 }).catch(() => {});
        } finally {
            entry.renderWarnings = entry.events.filter(event => /WebGL: INVALID|ArrayBufferView|srcOffset \+ length|resizable/.test(event.text)).length;
            entry.managedExceptions = entry.events.filter(event => /^ERROR: System\./.test(event.text)).map(event => event.text);
            entry.smokePassed = entry.events.some(event => event.text === 'REBIRTH_UI_SMOKE_PASS');
            entry.captureMarker = entry.events.some(event => event.text.startsWith('REBIRTH_CAPTURE_PASS'));
            report.checks.push(entry);
            await context.close();
        }
        return entry;
    }
    try {
        const redirect = await fetch(baseline.origin + '/TouhouSurvivor', { redirect: 'manual' });
        const wasm = await fetch(baseline.origin + '/TouhouSurvivor/index.wasm', { method: 'HEAD' });
        const hidden = await fetch(baseline.origin + '/project.godot');
        assert.equal(redirect.status, 308);
        assert.equal(redirect.headers.get('location'), '/TouhouSurvivor/');
        assert.equal(wasm.headers.get('content-type'), 'application/wasm');
        assert.equal(wasm.headers.get('cross-origin-embedder-policy'), 'require-corp');
        assert.equal(wasm.headers.get('cross-origin-opener-policy'), 'same-origin');
        assert.equal(hidden.status, 404);
        report.hosting = { redirect: true, wasmMime: true, isolationHeaders: true, projectFilesHidden: true, localhostOnly: true };
        browser = await playwright.chromium.launch({ headless: true, executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', args: ['--enable-unsafe-swiftshader'] });
        await capture('edge-4.7.1-title', baseline.origin);
        await capture('edge-4.6.1-title', compatibility.origin);
        await capture('edge-4.6.1-reimu-input', compatibility.origin, [], false, async (page, entry) => {
            await page.mouse.click(250, 410);
            await page.waitForTimeout(300);
            await page.mouse.click(380, 535);
            await page.waitForTimeout(1200);
            await page.keyboard.press('F3');
            await page.waitForTimeout(500);
            await page.screenshot({ path: path.join(output, 'edge-4.6.1-reimu-before.png') });
            await page.keyboard.down('KeyD');
            await page.waitForTimeout(1000);
            await page.keyboard.up('KeyD');
            await page.keyboard.press('Space');
            await page.waitForTimeout(400);
            entry.inputSequence = ['title click', 'Reimu click', 'F3', 'D held 1s', 'Space'];
        });
        await capture('edge-4.6.1-ui-smoke', compatibility.origin, ['--rebirth-smoke']);
        for (const mode of ['marisa-beam', 'choices', 'boss']) {
            await capture('edge-4.6.1-' + mode, compatibility.origin, [`--rebirth-capture=user://web-probe/${mode}.png`, `--rebirth-screen=${mode}`]);
        }
        await capture('edge-4.6.1-touch-emulation', compatibility.origin, [], true, async (page, entry) => {
            const dimensions = await page.evaluate(() => [innerWidth, innerHeight]);
            const scale = Math.min(dimensions[0] / 1280, dimensions[1] / 720);
            const left = (dimensions[0] - 1280 * scale) / 2;
            const top = (dimensions[1] - 720 * scale) / 2;
            await page.touchscreen.tap(left + 250 * scale, top + 410 * scale);
            await page.waitForTimeout(500);
            entry.touchSequence = ['tap title start button'];
            entry.movementControlsImplemented = false;
        });
        report.notes = ['Screenshots require visual review; scripted clicks alone do not assert game state.', 'Headless rendering is not a phone performance benchmark.', 'Capture fixtures use existing deterministic previews, not live full-run playthroughs.', 'No browser save persistence or physical phone pass is claimed.'];
    } finally {
        if (browser) await browser.close();
        baseline.server.closeAllConnections();
        compatibility.server.closeAllConnections();
        await Promise.all([new Promise(resolve => baseline.server.close(resolve)), new Promise(resolve => compatibility.server.close(resolve))]);
        await fs.writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2), 'utf8');
    }
    return report;
}

module.exports = { runBrowserProbe };

if (require.main === module) {
    runBrowserProbe(require('playwright'), path.resolve(__dirname, '../..')).then(report => {
        console.log(JSON.stringify({ overall: report.overall, checks: report.checks.map(({ name, renderWarnings, managedExceptions, smokePassed, captureMarker, automationError }) => ({ name, renderWarnings, managedExceptions, smokePassed, captureMarker, automationError })) }, null, 2));
        process.exitCode = report.overall === 'not-deployable' ? 2 : 0;
    }).catch(error => { console.error(error); process.exitCode = 1; });
}
