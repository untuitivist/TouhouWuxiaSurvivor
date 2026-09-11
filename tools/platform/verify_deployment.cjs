const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');

function startupTimeout(value = process.env.TOUHOU_PUBLIC_STARTUP_TIMEOUT_MS) {
    if (value === undefined) return 180000;
    const timeout = Number(value);
    assert.ok(Number.isInteger(timeout) && timeout >= 180000 && timeout <= 900000, 'Public startup timeout must be between 180000 and 900000 ms');
    return timeout;
}

function publicBrowserOptions() {
    return { headless: true, executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', args: ['--no-proxy-server', '--enable-unsafe-swiftshader'] };
}

function classifyBrowserConsole(events) {
    const errors = [];
    const startupDiagnostics = [];
    const waiting = ['still waiting on run dependencies:', 'dependency: wasm-instantiate', '(end of list)'];
    for (let index = 0; index < events.length; index++) {
        if (waiting.every((text, offset) => events[index + offset]?.startup === true && events[index + offset]?.type === 'error' && events[index + offset]?.text === text)) {
            startupDiagnostics.push(events.slice(index, index + waiting.length));
            index += waiting.length - 1;
        } else if (events[index].type === 'error' || /WebGL: INVALID|ArrayBufferView/.test(events[index].text)) {
            errors.push(events[index].text);
        }
    }
    return { errors, startupDiagnostics };
}

async function main() {
    const root = path.resolve(__dirname, '../..');
    const deployment = JSON.parse(await fs.readFile(path.join(root, 'artifacts/deployment/latest.json'), 'utf8'));
    const origin = new URL(deployment.url).origin;
    const output = path.join(deployment.output, 'public-verification');
    await fs.mkdir(output, { recursive: true });
    const startupBudgetMs = startupTimeout();
    const report = { url: deployment.url, releaseId: deployment.releaseId, physicalMobileTested: false, startupBudgetMs, networkMode: 'direct', proxyDisabledByBrowserFlag: true, checks: [] };
    let browser;
    try {
        const redirect = await fetch(origin + '/TouhouSurvivor', { redirect: 'manual' });
        assert.equal(redirect.status, 308);
        assert.equal(redirect.headers.get('location'), '/TouhouSurvivor/');
        for (const route of ['/', '/tusharedata/', '/test', '/test/anything']) {
            const response = await fetch(origin + route);
            assert.equal(response.status, route.startsWith('/test') ? 404 : 200);
            assert.equal(response.headers.get('cross-origin-opener-policy'), null);
            report.checks.push({ name: 'preserved-route', route, status: response.status });
            await response.arrayBuffer();
        }
        const entry = await fetch(deployment.url);
        const html = await entry.text();
        assert.equal(entry.status, 200);
        assert.equal(entry.headers.get('cache-control'), 'no-cache');
        assert.equal(entry.headers.get('cross-origin-opener-policy'), 'same-origin');
        assert.equal(entry.headers.get('cross-origin-embedder-policy'), 'require-corp');
        assert.ok(html.includes('/releases/' + deployment.releaseId + '/index'));
        assert.ok(html.includes('TouhouLoading.create(GODOT_CONFIG, TOUHOU_DOWNLOADS, false)'), 'The published entry must select the genuine threadless engine');
        const downloads = JSON.parse(html.match(/const TOUHOU_DOWNLOADS = (\[[^\n]+\]);/)[1]);
        assert.equal(downloads.length, 2);
        for (const file of downloads) {
            assert.equal(file.compressed, true);
            assert.ok(file.download.startsWith('/TouhouSurvivor/releases/' + deployment.releaseId + '/'));
            const response = await fetch(origin + file.download, { method: 'HEAD', headers: { 'Accept-Encoding': 'gzip, deflate, br' } });
            assert.equal(response.status, 200);
            assert.equal(response.headers.get('content-encoding'), null);
            assert.equal(Number(response.headers.get('content-length')), file.bytes);
            assert.ok(response.headers.get('cache-control').includes('immutable'));
            report.checks.push({ name: 'counted-download', file: file.download, bytes: file.bytes });
        }
        for (const name of ['index.wasm', 'index.pck', 'index.js']) {
            const response = await fetch(deployment.url + 'releases/' + deployment.releaseId + '/' + name, { method: 'HEAD', headers: { 'Accept-Encoding': 'gzip' } });
            assert.equal(response.status, 200);
            assert.equal(response.headers.get('content-encoding'), 'gzip');
            assert.ok(response.headers.get('cache-control').includes('immutable'));
            assert.equal(response.headers.get('cross-origin-embedder-policy'), 'require-corp');
            if (name.endsWith('.wasm')) assert.equal(response.headers.get('content-type'), 'application/wasm');
            report.checks.push({ name: 'compressed-asset', file: name, gzipBytes: Number(response.headers.get('content-length')) });
        }
        assert.equal((await fetch(deployment.url + 'missing-deployment-probe.wasm')).status, 404);
        browser = await chromium.launch(publicBrowserOptions());
        report.browser = browser.version();
        for (const { mobile, isolation } of [{ mobile: false, isolation: true }, { mobile: true, isolation: true }, { mobile: true, isolation: false }]) {
            const context = await browser.newContext({ viewport: mobile ? { width: 844, height: 390 } : { width: 1280, height: 720 }, hasTouch: mobile, isMobile: mobile });
            const page = await context.newPage();
            const check = { name: !isolation ? 'live-touch-no-isolation' : mobile ? 'live-touch' : 'live-normal-desktop', isolationHeadersRemovedForTest: !isolation, errors: [], failedRequests: [] };
            let startup = true;
            const consoleEvents = [];
            page.on('console', message => { consoleEvents.push({ type: message.type(), text: message.text(), startup }); });
            page.on('pageerror', error => check.errors.push(String(error)));
            page.on('requestfailed', request => check.failedRequests.push({ url: request.url(), error: request.failure()?.errorText }));
            const started = Date.now();
            try {
                if (mobile) {
                    await page.route(deployment.url, async route => {
                        const response = await route.fetch();
                        const body = await response.text();
                        assert.ok(body.includes('"args":[]'));
                        const headers = { ...response.headers() };
                        if (!isolation) {
                            delete headers['cross-origin-opener-policy'];
                            delete headers['cross-origin-embedder-policy'];
                        }
                        await route.fulfill({ response, headers, body: body.replace('"args":[]', '"args":["--","--web-validation"]') });
                    });
                }
                await page.goto(deployment.url, { waitUntil: 'domcontentloaded', timeout: 120000 });
                await page.waitForFunction(() => !document.querySelector('#loading'), undefined, { timeout: startupBudgetMs });
                startup = false;
                check.startupSeconds = (Date.now() - started) / 1000;
                check.capabilities = await page.evaluate(() => ({ isolated: crossOriginIsolated, sharedArrayBuffer: typeof SharedArrayBuffer }));
                assert.equal(check.capabilities.isolated, isolation);
                assert.equal(check.capabilities.sharedArrayBuffer, isolation ? 'function' : 'undefined');
                await page.screenshot({ path: path.join(output, check.name + '-title.png') });
                if (!mobile) {
                    assert.equal(await page.evaluate(() => typeof window.__touhouProbe), 'undefined');
                    await page.mouse.click(250, 410);
                    await page.waitForTimeout(300);
                    await page.mouse.click(380, 535);
                    await page.waitForTimeout(800);
                    await page.keyboard.down('KeyD');
                    await page.keyboard.press('Space');
                    await page.waitForTimeout(400);
                    await page.keyboard.up('KeyD');
                    await page.keyboard.press('F3');
                    await page.waitForTimeout(300);
                    await page.screenshot({ path: path.join(output, check.name + '-combat.png') });
                    await page.keyboard.press('Escape');
                } else {
                    await page.waitForFunction(() => window.__touhouProbe?.Screen === 'title');
                    const state = () => page.evaluate(() => window.__touhouProbe);
                    const point = async (horizontal, vertical) => page.evaluate(({ horizontal, vertical }) => {
                        const scale = Math.min(innerWidth / 1280, innerHeight / 720);
                        return { x: (innerWidth - 1280 * scale) / 2 + horizontal * scale, y: (innerHeight - 720 * scale) / 2 + vertical * scale };
                    }, { horizontal, vertical });
                    const click = async (text, name, fraction = 0.5) => {
                        const control = (await state()).Controls.find(control => name ? control.Name === name : control.Text === text);
                        assert.ok(control, 'Control found: ' + (name || text));
                        const location = await point(control.X + control.Width * fraction, control.Y + control.Height / 2);
                        await page.touchscreen.tap(location.x, location.y);
                        await page.waitForTimeout(300);
                    };
                    assert.equal((await state()).HasChineseGlyphs, true);
                    assert.equal((await state()).Persistent, true);
                    await click('游戏设置');
                    await click(null, 'master_volume', 0.42);
                    const volume = (await state()).MasterVolume;
                    assert.ok(volume > 0.35 && volume < 0.5);
                    await page.waitForTimeout(2000);
                    startup = true;
                    await page.reload({ waitUntil: 'domcontentloaded', timeout: 120000 });
                    await page.waitForFunction(() => window.__touhouProbe?.Screen === 'title' && !document.querySelector('#loading'), undefined, { timeout: startupBudgetMs });
                    startup = false;
                    assert.equal((await state()).MasterVolume, volume);
                    check.saveReloadPassed = true;
                    await click('开始游戏     →');
                    await click('选择 雾雨魔理沙');
                    await page.waitForFunction(() => window.__touhouProbe.TouchVisible);
                    const cdp = await context.newCDPSession(page);
                    const move = { id: 11, ...await point(235, 535) };
                    const focus = { id: 12, ...await point(988, 598) };
                    const dash = { id: 13, ...await point(1135, 548) };
                    await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [move] });
                    await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [move, focus] });
                    await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [move, focus, dash] });
                    await page.waitForFunction(() => window.__touhouProbe.MoveX > 0.8 && window.__touhouProbe.Focused && window.__touhouProbe.DashCooldown > 0);
                    await page.screenshot({ path: path.join(output, check.name + '-combat.png') });
                    await cdp.send('Input.dispatchTouchEvent', { type: 'touchCancel', touchPoints: [] });
                    await page.waitForFunction(() => window.__touhouProbe.MoveX === 0 && !window.__touhouProbe.TouchFocus);
                    await page.setViewportSize({ width: 390, height: 844 });
                    await page.waitForFunction(() => window.__touhouProbe.Screen === 'pause');
                    assert.equal(await page.locator('#rotate').isVisible(), true);
                    check.finalState = await state();
                    assert.equal(check.finalState.Warning, '');
                }
                assert.deepEqual(classifyBrowserConsole(consoleEvents).errors, []);
                assert.deepEqual(check.errors, []);
                assert.deepEqual(check.failedRequests, []);
                check.passed = true;
                console.log(check.name + ' PASS');
            } catch (error) {
                check.error = error.stack || String(error);
                check.passed = false;
                await page.screenshot({ path: path.join(output, check.name + '-failure.png') }).catch(() => {});
                throw error;
            } finally {
                check.elapsedSeconds = (Date.now() - started) / 1000;
                check.console = classifyBrowserConsole(consoleEvents);
                check.consoleEvents = consoleEvents;
                report.checks.push(check);
                await context.close();
            }
        }
        report.passed = true;
    } catch (error) {
        report.passed = false;
        report.error = error.stack || String(error);
        throw error;
    } finally {
        await browser?.close();
        await fs.writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2) + '\n', 'utf8');
    }
    console.log('PUBLIC_WEB_DEPLOYMENT_PASS ' + deployment.url);
}

module.exports = { classifyBrowserConsole, startupTimeout, publicBrowserOptions };
if (require.main === module) main().catch(error => { console.error(error); process.exitCode = 1; });
