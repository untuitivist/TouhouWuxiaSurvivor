const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');
const { startProbeServer } = require('../web_probe/server.cjs');

async function main() {
    const root = path.resolve(__dirname, '../..');
    const build = JSON.parse(await fs.readFile(path.join(root, 'artifacts/web-compatible-latest.json'), 'utf8'));
    const output = path.join(build.build, 'growth-verification', new Date().toISOString().replace(/[:.]/g, '-'));
    await fs.mkdir(output, { recursive: true });
    const report = { build: build.build, physicalDeviceTested: false, checks: [] };
    const browser = await chromium.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true });
    const host = await startProbeServer(build.site, 0, { isolation: false, entryArguments: ['--', '--web-validation', '--web-fixture=growth-choices'] });
    try {
        for (const scene of [
            { name: 'desktop', width: 1280, height: 720, touch: false, dpr: 1 },
            { name: 'phone', width: 844, height: 390, touch: true, dpr: 3 },
            { name: 'small-touch', width: 640, height: 360, touch: true, dpr: 1 }
        ]) {
            const context = await browser.newContext({ viewport: { width: scene.width, height: scene.height }, hasTouch: scene.touch, isMobile: scene.touch, deviceScaleFactor: scene.dpr });
            const check = { name: scene.name, errors: [] };
            report.checks.push(check);
            try {
                const page = await context.newPage();
                page.on('pageerror', error => check.errors.push(String(error)));
                page.on('console', message => { if (message.type() === 'error') check.errors.push(message.text()); });
                await page.goto(host.origin + '/TouhouSurvivor/', { waitUntil: 'domcontentloaded' });
                await page.waitForFunction(() => window.__touhouProbe?.Screen === 'choices' && !document.querySelector('#loading'), null, { timeout: 120000 });
                assert.equal(await page.evaluate(() => crossOriginIsolated), false);
                assert.equal(await page.evaluate(() => typeof SharedArrayBuffer), 'undefined');
                const state = () => page.evaluate(() => window.__touhouProbe);
                const click = async control => {
                    const box = await page.locator('#canvas').boundingBox();
                    const scale = Math.min(box.width / 1280, box.height / 720);
                    const horizontal = box.x + (box.width - 1280 * scale) / 2 + (control.X + control.Width / 2) * scale;
                    const vertical = box.y + (box.height - 720 * scale) / 2 + (control.Y + control.Height / 2) * scale;
                    if (scene.touch) await page.touchscreen.tap(horizontal, vertical);
                    else await page.mouse.click(horizontal, vertical);
                    await page.waitForTimeout(300);
                };
                const before = await state();
                const buttons = before.Controls.filter(control => control.Kind === 'Button');
                assert.equal(buttons.length, 4);
                for (const button of buttons) {
                    assert.ok(button.X >= 0 && button.Y >= 0 && button.X + button.Width <= 1280 && button.Y + button.Height <= 720, 'Choice controls fit logical viewport');
                }
                if (scene.touch) assert.ok(buttons.slice(0, 3).every(button => button.Height >= 78), 'Large touch choices');
                await page.screenshot({ path: path.join(output, scene.name + '-choices.png') });
                await click(buttons[3]);
                await page.waitForFunction(() => window.__touhouProbe?.Screen === 'build');
                assert.equal((await state()).Tick, before.Tick);
                await page.screenshot({ path: path.join(output, scene.name + '-build.png') });
                await click((await state()).Controls.find(control => control.Kind === 'Button'));
                await page.waitForFunction(() => window.__touhouProbe?.Screen === 'choices');
                assert.deepEqual((await state()).Controls.map(control => control.Text), before.Controls.map(control => control.Text));
                await click((await state()).Controls.find(control => control.Kind === 'Button'));
                assert.equal((await state()).Screen, 'choices', 'Queued level remains frozen after one choice');
                assert.equal((await state()).Tick, before.Tick);
                assert.deepEqual(check.errors, []);
                check.passed = true;
                console.log('WEB_GROWTH_PASS', scene.name);
            } finally { await context.close(); }
        }
        report.passed = true;
    } finally {
        await browser.close();
        host.server.closeAllConnections();
        await new Promise(resolve => host.server.close(resolve));
        await fs.writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2) + '\n', 'utf8');
    }
    console.log('WEB_GROWTH_VALIDATION_PASS', output);
}

main().catch(error => { console.error(error); process.exitCode = 1; });
