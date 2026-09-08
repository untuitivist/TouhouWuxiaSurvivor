const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');
const { startProbeServer } = require('../web_probe/server.cjs');

async function main() {
    const root = path.resolve(__dirname, '../..');
    const build = JSON.parse(await fs.readFile(path.join(root, 'artifacts/web-compatible-latest.json'), 'utf8'));
    const output = path.join(build.build, 'journal-verification', new Date().toISOString().replace(/[:.]/g, '-'));
    await fs.mkdir(output, { recursive: true });
    const report = { build: build.build, physicalDeviceTested: false, checks: [] };
    const browser = await chromium.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true });
    const host = await startProbeServer(build.site, 0, { isolation: false, entryArguments: ['--', '--web-validation'] });
    try {
        for (const scene of [
            { name: 'desktop', width: 1280, height: 720, touch: false, dpr: 1 },
            { name: 'phone', width: 844, height: 390, touch: true, dpr: 1 },
            { name: 'phone-dpr3', width: 844, height: 390, touch: true, dpr: 3 },
            { name: 'small-touch', width: 640, height: 360, touch: true, dpr: 1 }
        ]) {
            const context = await browser.newContext({ viewport: { width: scene.width, height: scene.height }, hasTouch: scene.touch, isMobile: scene.touch, deviceScaleFactor: scene.dpr });
            const check = { name: scene.name, errors: [], entries: [] };
            report.checks.push(check);
            try {
                const page = await context.newPage();
                page.on('pageerror', error => check.errors.push(String(error)));
                page.on('console', message => { if (message.type() === 'error') check.errors.push(message.text()); });
                page.on('requestfailed', request => check.errors.push(request.url() + ' ' + request.failure()?.errorText));
                await page.goto(host.origin + '/TouhouSurvivor/', { waitUntil: 'domcontentloaded' });
                await page.waitForFunction(() => window.__touhouProbe?.Screen === 'title' && !document.querySelector('#loading'), null, { timeout: 120000 });
                assert.equal(await page.evaluate(() => crossOriginIsolated), false);
                assert.equal(await page.evaluate(() => typeof SharedArrayBuffer), 'undefined');
                const state = () => page.evaluate(() => window.__touhouProbe);
                const waitScreen = expected => page.waitForFunction(value => window.__touhouProbe?.Screen === value, expected);
                const rectangle = async (horizontal, vertical, width, height) => {
                    const box = await page.locator('#canvas').boundingBox();
                    const scale = Math.min(box.width / 1280, box.height / 720);
                    return { x: box.x + (box.width - 1280 * scale) / 2 + horizontal * scale, y: box.y + (box.height - 720 * scale) / 2 + vertical * scale, width: width * scale, height: height * scale };
                };
                const click = async (value, byName = false) => {
                    const control = (await state()).Controls.find(item => (byName ? item.Name : item.Text) === value);
                    assert.ok(control, 'Missing control: ' + value);
                    const point = await rectangle(control.X + control.Width / 2, control.Y + control.Height / 2, 0, 0);
                    if (scene.touch) await page.touchscreen.tap(point.x, point.y);
                    else await page.mouse.click(point.x, point.y);
                    await page.waitForTimeout(220);
                };
                await click('夜境图鉴');
                await waitScreen('journal');
                await page.screenshot({ path: path.join(output, scene.name + '-cards.png') });
                for (let currentPage = 0; currentPage < 4; currentPage++) {
                    await page.screenshot({ path: path.join(output, scene.name + "-page-" + currentPage + ".png") });
                    const cards = (await state()).Controls.filter(control => control.Name.startsWith('journal_card_')).map(control => control.Name);
                    assert.equal(cards.length, currentPage === 3 ? 4 : 6);
                    for (const name of cards) {
                        await click(name, true);
                        await waitScreen('journal_detail');
                        check.entries.push(name);
                        if (name === 'journal_card_art-MasterSpark') {
                            await page.screenshot({ path: path.join(output, scene.name + '-detail.png') });
                            const clip = await rectangle(410, 172, 778, 390);
                            const top = await page.screenshot({ clip });
                            await click('向下阅读');
                            const lower = await page.screenshot({ clip });
                            assert.notDeepEqual(lower, top, 'Read-down button must scroll the actual text');
                            await page.screenshot({ path: path.join(output, scene.name + '-scrolled.png') });
                            await click('向上阅读');
                            assert.deepEqual(await page.screenshot({ clip }), top, 'Read-up restores original text');
                        }
                        await click('返回卡片');
                        await waitScreen('journal');
                        assert.deepEqual((await state()).Controls.filter(control => control.Name.startsWith('journal_card_')).map(control => control.Name), cards);
                    }
                    if (currentPage < 3) await click('下一页');
                }
                assert.equal(new Set(check.entries).size, 22);
                for (const [filter, count] of [['行者', 2], ['术式', 6], ['修习', 5], ['符卡', 2], ['妖怪', 5], ['夜境', 2]]) {
                    await click(filter);
                    const cards = (await state()).Controls.filter(control => control.Name.startsWith('journal_card_'));
                    assert.equal(cards.length, count);
                }
                await click('返回');
                await waitScreen('title');
                await click('踏入夜境     →');
                await waitScreen('heroes');
                await click('执此道 · 博丽灵梦');
                await waitScreen('playing');
                if (scene.touch) {
                    const pause = await rectangle(1180, 150, 0, 0);
                    await page.touchscreen.tap(pause.x, pause.y);
                } else await page.keyboard.press('Escape');
                await waitScreen('pause');
                const pausedTicks = (await state()).Tick;
                await click('夜境图鉴');
                await waitScreen('journal');
                await click((await state()).Controls.find(control => control.Name.startsWith('journal_card_')).Name, true);
                await waitScreen('journal_detail');
                await page.keyboard.press('Escape');
                await waitScreen('journal');
                await page.keyboard.press('Escape');
                await waitScreen('pause');
                assert.equal((await state()).Tick, pausedTicks);
                await click('继续行走');
                await waitScreen('playing');
                await page.waitForFunction(ticks => window.__touhouProbe.Tick > ticks, pausedTicks);
                assert.deepEqual(check.errors, []);
                check.passed = true;
                console.log('WEB_JOURNAL_PASS', scene.name, check.entries.length);
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
    console.log('WEB_JOURNAL_VALIDATION_PASS', output);
}

main().catch(error => { console.error(error); process.exitCode = 1; });
