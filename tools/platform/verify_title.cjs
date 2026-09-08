const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');
const { startProbeServer } = require('../web_probe/server.cjs');

async function main() {
    const root = path.resolve(__dirname, '../..');
    const build = JSON.parse(await fs.readFile(path.join(root, 'artifacts/web-compatible-latest.json'), 'utf8'));
    const output = path.join(build.build, 'aseprite-title-verification', new Date().toISOString().replace(/[:.]/g, '-'));
    await fs.mkdir(output, { recursive: true });
    const report = { build: build.build, physicalDeviceTested: false, checks: [] };
    const browser = await chromium.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true });
    const host = await startProbeServer(build.site, 0, { isolation: false, entryArguments: ['--', '--web-validation'] });
    try {
        for (const scene of [
            { name: 'desktop', width: 1280, height: 720, touch: false, dpr: 1 },
            { name: 'small', width: 640, height: 360, touch: false, dpr: 1 },
            { name: 'touch', width: 844, height: 390, touch: true, dpr: 3 }
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
                await page.waitForFunction(() => window.__touhouProbe?.Screen === 'title' && !document.querySelector('#loading'), null, { timeout: 120000 });
                const state = await page.evaluate(() => ({ ...window.__touhouProbe, isolated: crossOriginIsolated, shared: typeof SharedArrayBuffer }));
                assert.equal(state.isolated, false);
                assert.equal(state.shared, 'undefined');
                assert.ok(state.HasChineseGlyphs);
                const buttons = state.Controls.filter(control => control.Kind === 'Button');
                assert.ok(buttons.length >= 5);
                for (const button of buttons) assert.ok(button.X >= 0 && button.Y >= 0 && button.X + button.Width <= 1280 && button.Y + button.Height <= 720);
                await page.screenshot({ path: path.join(output, scene.name + '.png') });
                const box = await page.locator('#canvas').boundingBox();
                const scale = Math.min(box.width / 1280, box.height / 720);
                const pointX = box.x + (box.width - 1280 * scale) / 2 + (buttons[0].X + buttons[0].Width / 2) * scale;
                const pointY = box.y + (box.height - 720 * scale) / 2 + (buttons[0].Y + buttons[0].Height / 2) * scale;
                if (scene.touch) await page.touchscreen.tap(pointX, pointY); else await page.mouse.click(pointX, pointY);
                await page.waitForFunction(() => window.__touhouProbe?.Screen === 'heroes');
                assert.deepEqual(check.errors, []);
                check.passed = true;
                console.log('WEB_ASEPRITE_TITLE_PASS', scene.name);
            } finally { await context.close(); }
        }
        report.passed = true;
    } finally {
        await browser.close();
        host.server.closeAllConnections();
        await new Promise(resolve => host.server.close(resolve));
        await fs.writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2) + '\n', 'utf8');
    }
    console.log('WEB_ASEPRITE_TITLE_VALIDATION_PASS', output);
}
main().catch(error => { console.error(error); process.exitCode = 1; });
