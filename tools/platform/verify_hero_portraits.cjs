const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const { chromium } = require('playwright');
const { startProbeServer } = require('../web_probe/server.cjs');

async function main() {
    const root = path.resolve(__dirname, '../..');
    const build = JSON.parse(await fs.readFile(path.join(root, 'artifacts/web-compatible-latest.json'), 'utf8'));
    const output = path.join(build.build, 'hero-portrait-verification', new Date().toISOString().replace(/[:.]/g, '-'));
    await fs.mkdir(output, { recursive: true });
    const report = { build: build.build, physicalDeviceTested: false, sourceHashes: {}, checks: [] };
    for (const relative of ['game/presentation/HeroSelection.cs', 'assets/ui/portraits/ai-preview/reimu.png', 'assets/ui/portraits/ai-preview/marisa.png']) {
        const current = await fs.readFile(path.join(root, relative));
        const staged = await fs.readFile(path.join(build.build, 'stage', relative));
        assert.deepEqual(staged, current, 'Web uses the same maintained portrait input: ' + relative);
        report.sourceHashes[relative] = crypto.createHash('sha256').update(current).digest('hex');
    }
    let language = 'zh';
    const host = await startProbeServer(build.site, 0, { isolation: false, entryArguments: () => ['--', '--web-validation', '--web-language=' + language] });
    const browser = await chromium.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true });
    try {
        for (const scene of [
            { name: 'desktop-zh', width: 1280, height: 720, touch: false, dpr: 1, language: 'zh' },
            { name: 'desktop-en', width: 1280, height: 720, touch: false, dpr: 1, language: 'en' },
            { name: 'phone-zh-dpr3', width: 844, height: 390, touch: true, dpr: 3, language: 'zh' },
            { name: 'small-touch-en', width: 640, height: 360, touch: true, dpr: 1, language: 'en' }
        ]) {
            language = scene.language;
            const context = await browser.newContext({ viewport: { width: scene.width, height: scene.height }, hasTouch: scene.touch, isMobile: scene.touch, deviceScaleFactor: scene.dpr });
            const check = { name: scene.name, errors: [], heroes: [] };
            report.checks.push(check);
            try {
                const page = await context.newPage();
                page.on('pageerror', error => check.errors.push(String(error)));
                page.on('console', message => { if (message.type() === 'error') check.errors.push(message.text()); });
                page.on('requestfailed', request => check.errors.push(request.url() + ' ' + request.failure()?.errorText));
                const state = () => page.evaluate(() => window.__touhouProbe);
                const waitScreen = expected => page.waitForFunction(value => window.__touhouProbe?.Screen === value && !document.querySelector('#loading'), expected, { timeout: 120000 });
                const click = async control => {
                    assert.ok(control, 'Expected an actual UI control');
                    const canvas = await page.locator('#canvas').boundingBox();
                    const scale = Math.min(canvas.width / 1280, canvas.height / 720);
                    const horizontal = canvas.x + (canvas.width - 1280 * scale) / 2 + (control.X + control.Width / 2) * scale;
                    const vertical = canvas.y + (canvas.height - 720 * scale) / 2 + (control.Y + control.Height / 2) * scale;
                    if (scene.touch) await page.touchscreen.tap(horizontal, vertical);
                    else await page.mouse.click(horizontal, vertical);
                };
                await page.goto(host.origin + '/TouhouSurvivor/', { waitUntil: 'domcontentloaded' });
                await waitScreen('title');
                assert.equal(await page.evaluate(() => crossOriginIsolated), false);
                assert.equal(await page.evaluate(() => typeof SharedArrayBuffer), 'undefined');
                assert.equal((await state()).Language, scene.language);
                await click((await state()).Controls[0]);
                await waitScreen('heroes');
                const initial = await state();
                const choices = initial.Controls.filter(control => control.Name.startsWith('choose_'));
                assert.equal(choices.length, 2);
                for (const control of choices) {
                    assert.ok(control.Height >= (scene.touch ? 76 : 48));
                    assert.ok(control.X >= 0 && control.Y >= 0 && control.X + control.Width <= 1280 && control.Y + control.Height <= 720);
                }
                await page.screenshot({ path: path.join(output, scene.name + '.png') });
                await click(initial.Controls.find(control => !control.Name.startsWith('choose_')));
                await waitScreen('title');
                check.returnPassed = true;
                for (const hero of ['Reimu', 'Marisa']) {
                    await click((await state()).Controls[0]);
                    await waitScreen('heroes');
                    await click((await state()).Controls.find(control => control.Name === 'choose_' + hero));
                    await waitScreen('playing');
                    assert.equal((await state()).Hero, hero);
                    check.heroes.push(hero);
                    if (hero === 'Reimu') {
                        await page.reload({ waitUntil: 'domcontentloaded' });
                        await waitScreen('title');
                    }
                }
                assert.deepEqual(check.errors, []);
                check.passed = true;
            } catch (error) {
                check.passed = false;
                check.error = String(error.stack || error);
                const page = context.pages()[0];
                if (page) await page.screenshot({ path: path.join(output, scene.name + '-failure.png') }).catch(() => {});
            } finally {
                await fs.writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2) + '\n', 'utf8');
                await context.close();
            }
            console.log(scene.name, check.passed ? 'PASS' : check.error);
        }
    } finally {
        await browser.close();
        host.server.closeAllConnections();
        await new Promise(resolve => host.server.close(resolve));
    }
    assert.ok(report.checks.every(check => check.passed), 'All portrait-selection browser checks must pass');
    console.log('HERO_PORTRAITS_WEB_PASS ' + output);
}

main().catch(error => { console.error(error); process.exitCode = 1; });
