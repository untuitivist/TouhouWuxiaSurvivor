const fs = require('node:fs/promises');
const path = require('node:path');
const assert = require('node:assert/strict');
const { startProbeServer } = require('../web_probe/server.cjs');
const { startDeploymentFixture } = require('./deployment_fixture.cjs');

async function verify(playwright, repository, { compatible = false, isolation = true, deployment = false } = {}) {
    const latest = JSON.parse(await fs.readFile(path.join(repository, compatible ? 'artifacts/web-compatible-latest.json' : 'artifacts/web-latest.json'), 'utf8'));
    const raw = !deployment && (compatible || process.argv.includes('--raw'));
    const output = path.join(latest.build, compatible ? `verification-compatible-${deployment ? 'deployment-' : ''}${isolation ? 'isolated' : 'unisolated'}` : raw ? 'verification-raw' : 'verification');
    await fs.mkdir(output, { recursive: true });
    let launchArguments = [];
    const hostOptions = { isolation, entryArguments: () => launchArguments };
    const host = raw ? await startProbeServer(latest.site, 0, hostOptions) : await startDeploymentFixture(latest, hostOptions);
    const report = { physicalMobileTested: false, build: latest.build, transferMode: host.transferMode || 'raw', checks: [] };
    let browser;
    async function scenario(name, mobile, args, actions) {
        const context = await browser.newContext({ viewport: mobile ? { width: 844, height: 390 } : { width: 1280, height: 720 }, hasTouch: mobile, isMobile: mobile });
        const page = await context.newPage();
        const entry = { name, mobileEmulation: mobile, events: [], failedRequests: [] };
        page.on('console', message => { if (['warning', 'error'].includes(message.type())) entry.events.push({ type: message.type(), text: message.text() }); });
        page.on('pageerror', error => entry.events.push({ type: 'pageerror', text: String(error) }));
        page.on('requestfailed', request => entry.failedRequests.push({ url: request.url(), error: request.failure()?.errorText }));
        launchArguments = ['--', '--web-validation', ...args];
        const wait = async predicate => {
            try { await page.waitForFunction(predicate, undefined, { timeout: 45000 }); }
            catch (error) { throw new Error(`${predicate}: ${error}`); }
        };
        const state = () => page.evaluate(() => window.__touhouProbe);
        async function point(horizontal, vertical) {
            return page.evaluate(({ horizontal, vertical }) => {
                const scale = Math.min(innerWidth / 1280, innerHeight / 720);
                return { x: (innerWidth - 1280 * scale) / 2 + horizontal * scale, y: (innerHeight - 720 * scale) / 2 + vertical * scale };
            }, { horizontal, vertical });
        }
        async function click(text, name = null, fraction = 0.5) {
            await page.waitForFunction(({ text, name }) => window.__touhouProbe.Controls.some(control => name ? control.Name === name : control.Text === text), { text, name });
            const control = (await state()).Controls.find(control => name ? control.Name === name : control.Text === text);
            const position = await point(control.X + control.Width * fraction, control.Y + control.Height / 2);
            if (mobile) await page.touchscreen.tap(position.x, position.y);
            else await page.mouse.click(position.x, position.y);
            await page.waitForTimeout(250);
        }
        const screenshot = async suffix => page.screenshot({ path: path.join(output, name + '-' + suffix + '.png') });
        try {
            await page.goto(host.origin + '/TouhouSurvivor/', { waitUntil: 'domcontentloaded' });
            await wait(() => !!window.__touhouProbe && !document.querySelector('#loading'));
            assert.equal(await page.evaluate(() => crossOriginIsolated), isolation);
            if (!isolation) assert.equal(await page.evaluate(() => typeof SharedArrayBuffer), 'undefined');
            assert.equal((await state()).HasChineseGlyphs, true);
            assert.equal((await state()).Persistent, true);
            await actions({ page, context, wait, state, point, click, screenshot, entry });
            entry.finalState = await state();
            assert.equal(entry.finalState.Warning, '');
            assert.deepEqual(entry.events.filter(event => event.type !== 'warning' || /WebGL: INVALID|ArrayBufferView/.test(event.text)), []);
            assert.deepEqual(entry.failedRequests, []);
            entry.passed = true;
        } catch (error) {
            entry.error = error.stack || String(error);
            await screenshot('failure').catch(() => {});
            entry.finalState = await state().catch(() => null);
            entry.passed = false;
        } finally {
            report.checks.push(entry);
            await fs.writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2) + '\n', 'utf8');
            console.log(name, entry.passed ? 'PASS' : entry.error);
            await context.close();
        }
    }
    try {
        browser = await playwright.chromium.launch({ headless: true, executablePath: process.env.WEB_TEST_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', args: ['--enable-unsafe-swiftshader'] });
        report.browser = browser.version();
        const redirect = await fetch(host.origin + '/TouhouSurvivor', { redirect: 'manual' });
        assert.equal(redirect.status, 308);
        const wasm = await fetch(host.origin + (host.wasmPath || '/TouhouSurvivor/index.wasm'), { method: 'HEAD' });
        assert.equal(wasm.headers.get('content-type'), 'application/wasm');
        assert.equal(wasm.headers.get('cross-origin-opener-policy'), isolation ? 'same-origin' : null);
        assert.equal(wasm.headers.get('cross-origin-embedder-policy'), isolation ? 'require-corp' : null);
        assert.equal((await fetch(host.origin + '/project.godot')).status, 404);
        report.hostingPassed = true;
        await scenario('desktop', false, [], async ({ page, wait, state, click, screenshot, entry }) => {
            await screenshot('title');
            await click('游戏设置');
            await click(null, 'master_volume', 0.37);
            const volume = (await state()).MasterVolume;
            assert.ok(volume > 0.3 && volume < 0.45);
            await page.waitForTimeout(2000);
            await page.reload({ waitUntil: 'domcontentloaded' });
            await wait(() => !!window.__touhouProbe && !document.querySelector('#loading'));
            assert.equal((await state()).MasterVolume, volume);
            entry.saveReloadPassed = true;
            await click('踏入夜境     →');
            await click('执此道 · 博丽灵梦');
            await wait(() => window.__touhouProbe.Screen === 'playing');
            const before = await state();
            await page.keyboard.down('KeyD');
            await page.keyboard.down('Shift');
            await wait(() => window.__touhouProbe.Focused);
            await page.keyboard.press('Space');
            await wait(() => window.__touhouProbe.DashCooldown > 0);
            await page.waitForTimeout(250);
            await page.keyboard.up('KeyD');
            await page.keyboard.up('Shift');
            assert.ok((await state()).X > before.X);
            await page.keyboard.press('F3');
            await wait(() => window.__touhouProbe.DebugVisible);
            await screenshot('combat');
            await page.keyboard.press('Escape');
            await wait(() => window.__touhouProbe.Screen === 'pause');
            await page.keyboard.press('KeyE');
            await wait(() => window.__touhouProbe.Screen === 'build');
            await page.keyboard.press('Escape');
            await wait(() => window.__touhouProbe.Screen === 'pause');
            await click('游戏设置');
            await click('画面');
            assert.ok(!(await state()).Controls.some(control => /window_mode|resolution|vsync/.test(control.Name)));
            await screenshot('video');
        });
        await scenario('touch', true, [], async ({ page, context, wait, state, point, click, screenshot, entry }) => {
            await screenshot('title');
            const start = (await state()).Controls.find(control => control.Text.includes('踏入'));
            await click(start.Text);
            await click('执此道 · 雾雨魔理沙');
            await wait(() => window.__touhouProbe.TouchVisible);
            const cdp = await context.newCDPSession(page);
            const move = { id: 11, ...await point(235, 535) };
            const focus = { id: 12, ...await point(988, 598) };
            const dash = { id: 13, ...await point(1135, 548) };
            await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [move] });
            await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [move, focus] });
            await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [move, focus, dash] });
            await wait(() => window.__touhouProbe.MoveX > 0.8 && window.__touhouProbe.Focused && window.__touhouProbe.DashCooldown > 0);
            await screenshot('three-fingers');
            await cdp.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [focus, dash] });
            await wait(() => !window.__touhouProbe.TouchFocus && window.__touhouProbe.MoveX > 0.8);
            await cdp.send('Input.dispatchTouchEvent', { type: 'touchCancel', touchPoints: [] });
            await wait(() => window.__touhouProbe.MoveX === 0 && !window.__touhouProbe.TouchFocus);
            entry.multiTouchPassed = true;
            await page.setViewportSize({ width: 390, height: 844 });
            await wait(() => window.__touhouProbe.Screen === 'pause');
            assert.equal(await page.locator('#rotate').isVisible(), true);
            await page.setViewportSize({ width: 844, height: 390 });
            await page.waitForTimeout(250);
            assert.equal((await state()).Screen, 'pause');
            await click('游戏设置');
            await click('触控');
            await click('切换诊断信息（F3）');
            await wait(() => window.__touhouProbe.DebugVisible);
            await screenshot('settings');
            await click('返回');
            await wait(() => window.__touhouProbe.Screen === 'pause');
            await click('继续行走');
            await wait(() => window.__touhouProbe.Screen === 'playing');
            const pause = await point(1180, 150);
            await page.touchscreen.tap(pause.x, pause.y);
            await wait(() => window.__touhouProbe.Screen === 'pause');
        });
        await scenario('touch-upgrade', true, ['--web-fixture=choices'], async ({ wait, click, screenshot }) => {
            await wait(() => window.__touhouProbe.Phase === 'Choosing');
            await screenshot('offers');
            await click('领悟此式');
            await click('领悟此式');
            await wait(() => window.__touhouProbe.Phase === 'Playing');
        });
        for (const hero of ['reimu', 'marisa']) {
            await scenario(hero + '-journey', false, ['--web-pilot', ...(hero === 'marisa' ? ['--web-marisa'] : [])], async ({ page, state, screenshot, entry }) => {
                await page.waitForFunction(() => window.__touhouProbe.BossSpawned, undefined, { timeout: 300000 });
                entry.bossSeen = (await state()).BossPresent;
                await screenshot('boss');
                await page.waitForFunction(() => window.__touhouProbe.Screen === 'result', undefined, { timeout: 300000 });
                assert.equal((await state()).Phase, 'Won');
                assert.ok((await state()).CompletedRuns > 0);
                await screenshot('result');
            });
        }
        report.passed = report.checks.every(entry => entry.passed);
    } finally {
        await browser?.close();
        host.server.closeAllConnections();
        await new Promise(resolve => host.server.close(resolve));
        await fs.writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2) + '\n', 'utf8');
    }
    assert.equal(report.passed, true, 'See verification/report.json for failures');
    console.log('SHARED_WEB_VALIDATION_PASS', output);
}

if (require.main === module) verify(require('playwright'), path.resolve(__dirname, '../..')).catch(error => { console.error(error); process.exitCode = 1; });
module.exports = { verify };
