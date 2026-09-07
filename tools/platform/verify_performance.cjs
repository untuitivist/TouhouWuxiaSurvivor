const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');
const { startDeploymentFixture } = require('./deployment_fixture.cjs');

async function main() {
    const root = path.resolve(__dirname, '../..');
    const build = JSON.parse(await fs.readFile(path.join(root, process.argv.includes('--compatible') ? 'artifacts/web-compatible-latest.json' : 'artifacts/web-latest.json'), 'utf8'));
    const output = path.join(build.build, 'performance-verification');
    await fs.mkdir(output, { recursive: true });
    const host = await startDeploymentFixture(build);
    const browser = await chromium.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true, args: ['--enable-unsafe-swiftshader'] });
    const report = { browser: browser.version(), transferMode: host.transferMode, physicalDeviceTested: false, scope: 'Desktop Edge with touch/DPR emulation, deterministic static stress fixture; not phone FPS.', checks: [] };
    try {
        for (const density of [1, 3]) {
            const context = await browser.newContext({ viewport: { width: 844, height: 390 }, hasTouch: true, isMobile: true, deviceScaleFactor: density });
            const page = await context.newPage();
            const errors = [];
            page.on('pageerror', error => errors.push(String(error)));
            page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
            page.on('requestfailed', request => errors.push(request.url() + ' ' + request.failure()?.errorText));
            await page.route('**/TouhouSurvivor/', async route => {
                const response = await route.fetch();
                const html = await response.text();
                assert.ok(html.includes('"args":[]'));
                await route.fulfill({ response, body: html.replace('"args":[]', '"args":["--","--web-validation","--web-fixture=performance"]') });
            });
            await page.goto(host.origin + '/TouhouSurvivor/', { waitUntil: 'domcontentloaded' });
            await page.waitForFunction(() => window.__touhouProbe?.BatchInstances === 2320 && !document.getElementById('loading'), {}, { timeout: 120000 });
            await page.waitForTimeout(5000);
            const state = await page.evaluate(() => ({ ...window.__touhouProbe, canvasWidth: document.getElementById('canvas').width, canvasHeight: document.getElementById('canvas').height, devicePixelRatio }));
            assert.equal(state.RenderWidth, 1280);
            assert.equal(state.RenderHeight, 720);
            assert.ok(state.DrawCalls > 0 && state.DrawCalls < 1200, JSON.stringify(state));
            assert.equal(state.Tick, 0, 'Static fixture must not simulate hidden gameplay');
            assert.deepEqual(errors, []);
            await page.screenshot({ path: path.join(output, 'touch-dpr-' + density + '.png') });
            report.checks.push({ density, passed: true, state, errors });
            console.log('WEB_PERFORMANCE_PASS', density, JSON.stringify({ render: [state.RenderWidth, state.RenderHeight], calls: state.DrawCalls, fps: state.Fps, batchMs: state.BatchMilliseconds }));
            await context.close();
        }
    } finally {
        await browser.close();
        host.server.closeAllConnections();
        await new Promise(resolve => host.server.close(resolve));
        await fs.writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2), 'utf8');
    }
    console.log('WEB_RENDER_VALIDATION_PASS', output);
}

main().catch(error => { console.error(error); process.exitCode = 1; });
