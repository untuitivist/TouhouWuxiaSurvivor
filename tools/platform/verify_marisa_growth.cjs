const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');
const { startDeploymentFixture } = require('./deployment_fixture.cjs');
const { verificationBuildPath, verificationOutputRoot } = require('./verification_paths.cjs');

async function main() {
    const root = path.resolve(__dirname, '../..');
    const build = JSON.parse(await fs.readFile(verificationBuildPath(root, 'artifacts/web-compatible-latest.json'), 'utf8'));
    const output = path.join(verificationOutputRoot(build), 'marisa-growth');
    await fs.mkdir(output, { recursive: true });
    let args = [];
    const host = await startDeploymentFixture(build, { isolation: false, entryArguments: () => args });
    const browser = await chromium.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true });
    const report = { build: build.build, physicalDeviceTested: false, checks: [], passed: false };
    try {
        for (const layout of [
            { name: 'desktop-zh', width: 1280, height: 720, language: 'zh', touch: false },
            { name: 'desktop-en', width: 1280, height: 720, language: 'en', touch: false },
            { name: 'small-touch-zh', width: 640, height: 360, language: 'zh', touch: true },
            { name: 'touch-en', width: 844, height: 390, language: 'en', touch: true }
        ]) {
            args = ['--', '--web-validation', '--web-language=' + layout.language, '--web-fixture=marisa-growth-choices'];
            const context = await browser.newContext({ viewport: { width: layout.width, height: layout.height }, hasTouch: layout.touch, isMobile: layout.touch });
            const check = { name: layout.name, errors: [], passed: false };
            report.checks.push(check);
            try {
                const page = await context.newPage();
                page.on('pageerror', error => check.errors.push(String(error)));
                page.on('console', message => { if (message.type() === 'error') check.errors.push(message.text()); });
                page.on('requestfailed', request => check.errors.push(request.url() + ': ' + request.failure()?.errorText));
                const state = () => page.evaluate(() => window.__touhouProbe);
                const ready = screen => page.waitForFunction(screen => window.__touhouProbe?.Screen === screen && !document.getElementById('loading'), screen, { timeout: 120000 });
                const click = async control => {
                    assert.ok(control, 'Expected control exists');
                    const box = await page.locator('#canvas').boundingBox();
                    const scale = Math.min(box.width / 1280, box.height / 720);
                    const horizontal = box.x + (box.width - 1280 * scale) / 2 + (control.X + control.Width / 2) * scale;
                    const vertical = box.y + (box.height - 720 * scale) / 2 + (control.Y + control.Height / 2) * scale;
                    if (layout.touch) await page.touchscreen.tap(horizontal, vertical);
                    else await page.mouse.click(horizontal, vertical);
                    await page.waitForTimeout(300);
                };
                await page.goto(host.origin + '/TouhouSurvivor/', { waitUntil: 'domcontentloaded' });
                await ready('choices');
                assert.deepEqual(await page.evaluate(() => [crossOriginIsolated, typeof SharedArrayBuffer]), [false, 'undefined']);
                const before = await state();
                assert.equal(before.Hero, 'Marisa');
                assert.equal(before.Language, layout.language);
                assert.equal(before.SignatureUnlocked, false);
                assert.deepEqual(before.ChoiceIds, ['marisa.stars.pierce', 'marisa.stardust.echo', 'marisa.masterspark.sweep']);
                const buttons = before.Controls.filter(control => control.Kind === 'Button');
                assert.equal(buttons.length, 4);
                assert.ok(buttons.every(control => control.X >= 0 && control.Y >= 0 && control.X + control.Width <= 1280 && control.Y + control.Height <= 720));
                await page.screenshot({ path: path.join(output, layout.name + '-choices.png') });
                await click(buttons[3]);
                await ready('build');
                assert.equal((await state()).Tick, before.Tick);
                await page.screenshot({ path: path.join(output, layout.name + '-pending-build.png') });
                await click((await state()).Controls.find(control => control.Kind === 'Button'));
                await ready('choices');
                assert.deepEqual((await state()).ChoiceIds, before.ChoiceIds);
                await click((await state()).Controls.find(control => control.Kind === 'Button'));
                assert.ok(((await state()).Traits & 128) !== 0, 'Actual UI choice grants Star Pierce');
                assert.equal((await state()).Tick, before.Tick, 'Queued choices still freeze combat');
                for (let index = 0; index < 5 && (await state()).Screen === 'choices'; index++)
                    await click((await state()).Controls.find(control => control.Kind === 'Button'));
                await ready('playing');
                await page.waitForFunction(tick => window.__touhouProbe.Tick > tick + 30, before.Tick);
                args = ['--', '--web-validation', '--web-language=' + layout.language, '--web-fixture=marisa-growth-build'];
                await page.reload();
                await ready('build');
                const combined = await state();
                assert.equal(combined.Traits, 8064, 'All six compatible Marisa behavior traits are present');
                assert.equal(combined.SignatureUnlocked, false);
                assert.deepEqual(combined.AbilityRanks.slice(0, 6), [0, 0, 0, 1, 1, 1]);
                await page.screenshot({ path: path.join(output, layout.name + '-combined-build.png') });
                await click(combined.Controls.find(control => control.Kind === 'Button'));
                await ready('playing');
                await page.waitForFunction(tick => window.__touhouProbe.Tick > tick + 60 && window.__touhouProbe.Projectiles > 0, combined.Tick);
                await page.screenshot({ path: path.join(output, layout.name + '-combat.png') });
                await page.keyboard.press('KeyE');
                await ready('build');
                const pausedTick = (await state()).Tick;
                await page.waitForTimeout(500);
                assert.equal((await state()).Tick, pausedTick, 'Inspecting combined routes freezes the live game');
                assert.deepEqual(check.errors, []);
                check.passed = true;
                console.log('MARISA_GROWTH_PASS', layout.name);
            } finally { await context.close(); }
        }
        report.passed = true;
    } finally {
        await browser.close();
        host.server.closeAllConnections();
        await new Promise(resolve => host.server.close(resolve));
        await fs.writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2) + '\n', 'utf8');
    }
    console.log('MARISA_GROWTH_WEB_PASS', output);
}

main().catch(error => { console.error(error); process.exitCode = 1; });
