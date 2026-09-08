const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');
const { startProbeServer } = require('../web_probe/server.cjs');

async function main() {
    const root = path.resolve(__dirname, '../..');
    const build = JSON.parse(await fs.readFile(path.join(root, 'artifacts/web-compatible-latest.json'), 'utf8'));
    const output = path.join(build.build, 'minimap-verification', new Date().toISOString().replace(/[:.]/g, '-'));
    await fs.mkdir(output, { recursive: true });
    const report = { build: build.build, physicalDeviceTested: false, checks: [] };
    const browser = await chromium.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true });
    const host = await startProbeServer(build.site, 0, { isolation: false, entryArguments: ['--', '--web-validation', '--web-fixture=marisa-beam'] });
    try {
        for (const scene of [
            { name: 'desktop', width: 1280, height: 720, touch: false, dpr: 1 },
            { name: 'phone', width: 844, height: 390, touch: true, dpr: 1 },
            { name: 'phone-dpr3', width: 844, height: 390, touch: true, dpr: 3 },
            { name: 'small-touch', width: 640, height: 360, touch: true, dpr: 1 }
        ]) {
            const context = await browser.newContext({ viewport: { width: scene.width, height: scene.height }, hasTouch: scene.touch, isMobile: scene.touch, deviceScaleFactor: scene.dpr });
            const check = { name: scene.name, errors: [] };
            report.checks.push(check);
            try {
                const page = await context.newPage();
                page.on('pageerror', error => check.errors.push(String(error)));
                page.on('console', message => { if (message.type() === 'error') check.errors.push(message.text()); });
                page.on('requestfailed', request => check.errors.push(request.url() + ' ' + request.failure()?.errorText));
                await page.goto(host.origin + '/TouhouSurvivor/', { waitUntil: 'domcontentloaded' });
                await page.waitForFunction(() => window.__touhouProbe?.Screen === 'playing' && !document.querySelector('#loading'), null, { timeout: 120000 });
                check.state = await page.evaluate(() => ({ ...window.__touhouProbe, isolated: crossOriginIsolated, sharedMemory: typeof SharedArrayBuffer }));
                assert.equal(check.state.isolated, false);
                assert.equal(check.state.sharedMemory, 'undefined');
                assert.equal(check.state.TouchVisible, scene.touch);
                assert.equal(check.state.MinimapY, scene.touch ? 209 : 108);
                assert.equal(check.state.MinimapX, 1111);
                assert.equal(check.state.MinimapWidth, 141);
                assert.equal(check.state.MinimapHeight, 111);
                assert.equal(check.state.RenderWidth, 1280);
                assert.equal(check.state.RenderHeight, 720);
                await page.screenshot({ path: path.join(output, scene.name + '.png') });
                if (scene.touch) {
                    assert.ok(check.state.MinimapY > 191);
                    const box = await page.locator('#canvas').boundingBox();
                    const scale = Math.min(box.width / 1280, box.height / 720);
                    const tap = async (horizontal, vertical) => page.touchscreen.tap(box.x + (box.width - 1280 * scale) / 2 + horizontal * scale, box.y + (box.height - 720 * scale) / 2 + vertical * scale);
                    await tap(1180, 150);
                    await page.waitForFunction(() => window.__touhouProbe?.Screen === 'pause');
                    await page.keyboard.press('Escape');
                    await page.waitForFunction(() => window.__touhouProbe?.Screen === 'playing' && window.__touhouProbe.MinimapY === 209);
                    await tap(1026, 150);
                    await page.waitForFunction(() => window.__touhouProbe?.Screen === 'build');
                    await page.keyboard.press('Escape');
                    await page.waitForFunction(() => window.__touhouProbe?.Screen === 'playing' && window.__touhouProbe.MinimapY === 209);
                    check.touchButtonsPassed = true;
                }
                assert.deepEqual(check.errors, []);
                check.passed = true;
                console.log('WEB_MINIMAP_PASS', scene.name);
            } finally {
                await context.close();
            }
        }
        report.passed = true;
    } finally {
        await browser.close();
        host.server.closeAllConnections();
        await new Promise(resolve => host.server.close(resolve));
        await fs.writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2) + '\n', 'utf8');
    }
    console.log('WEB_MINIMAP_VALIDATION_PASS', output);
}

main().catch(error => { console.error(error); process.exitCode = 1; });
