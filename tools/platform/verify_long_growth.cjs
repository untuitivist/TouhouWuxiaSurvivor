const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');
const { startDeploymentFixture } = require('./deployment_fixture.cjs');

async function main() {
    const root = path.resolve(__dirname, '../..');
    const build = JSON.parse(await fs.readFile(path.join(root, 'artifacts/web-compatible-latest.json'), 'utf8'));
    const output = path.join(build.build, 'long-growth-verification', new Date().toISOString().replace(/[:.]/g, '-'));
    await fs.mkdir(output, { recursive: true });
    const report = { build: build.build, physicalMobileTested: false, checks: [], passed: false };
    let argumentsForPage = [];
    const host = await startDeploymentFixture(build, { isolation: false, entryArguments: () => argumentsForPage });
    const browser = await chromium.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true });
    async function scenario(name, language, mobile, fixture, exercise) {
        argumentsForPage = ['--', '--web-validation', '--web-language=' + language, '--web-fixture=' + fixture];
        const context = await browser.newContext({ viewport: mobile ? { width: 844, height: 390 } : { width: 1280, height: 720 }, isMobile: mobile, hasTouch: mobile });
        const page = await context.newPage();
        const entry = { name, language, mobileEmulation: mobile, errors: [], passed: false };
        report.checks.push(entry);
        page.on('pageerror', error => entry.errors.push(String(error)));
        page.on('console', message => { if (message.type() === 'error') entry.errors.push(message.text()); });
        const state = () => page.evaluate(() => window.__touhouProbe);
        const ready = async () => {
            await page.waitForFunction(() => window.__touhouProbe && !document.querySelector('#loading'), null, { timeout: 60000 });
            assert.equal(await page.evaluate(() => crossOriginIsolated), false);
            assert.equal(await page.evaluate(() => typeof SharedArrayBuffer), 'undefined');
            assert.equal((await state()).Language, language);
        };
        async function click(nameOrText) {
            const control = (await state()).Controls.find(control => control.Name === nameOrText || control.Text === nameOrText);
            assert.ok(control, 'Missing control: ' + nameOrText);
            const position = await page.evaluate(control => {
                const scale = Math.min(innerWidth / 1280, innerHeight / 720);
                return { x: (innerWidth - 1280 * scale) / 2 + (control.X + control.Width / 2) * scale,
                    y: (innerHeight - 720 * scale) / 2 + (control.Y + control.Height / 2) * scale };
            }, control);
            if (mobile) await page.touchscreen.tap(position.x, position.y);
            else await page.mouse.click(position.x, position.y);
        }
        try {
            await page.goto(host.origin + '/TouhouSurvivor/', { waitUntil: 'domcontentloaded' });
            await ready();
            await exercise({ page, state, click, ready, entry });
            assert.deepEqual(entry.errors, []);
            entry.passed = true;
            console.log('LONG_GROWTH_CHECK', name);
        } catch (error) {
            entry.failure = String(error);
            entry.state = await state().catch(() => null);
            await page.screenshot({ path: path.join(output, name + '-failure.png'), timeout: 10000 }).catch(() => {});
            throw error;
        } finally { await context.close(); }
    }
    try {
        for (const language of ['zh', 'en']) for (const mobile of [false, true]) for (const hero of ['reimu', 'marisa']) {
            const name = hero + '-' + language + (mobile ? '-touch' : '-desktop');
            await scenario(name, language, mobile, 'free-growth-' + hero, async ({ page, state, click, entry }) => {
                const before = await state();
                assert.equal(before.Phase, 'Choosing');
                assert.equal(before.ChoiceIds.length, 3);
                assert.ok(before.ChoiceIds.every(id => !id.includes('.stance.')));
                assert.notEqual(before.ChoiceAbilities[0], before.ChoiceAbilities[1]);
                assert.equal(Object.hasOwn(before, 'Stance'), false);
                assert.equal(Object.hasOwn(before, 'PendingStance'), false);
                await page.screenshot({ path: path.join(output, name + '-choices.png') });
                await click('growth_inspect');
                await page.waitForFunction(() => window.__touhouProbe.Screen === 'build');
                await page.screenshot({ path: path.join(output, name + '-build.png') });
                assert.deepEqual((await state()).ChoiceIds, before.ChoiceIds);
                assert.equal((await state()).PendingChoices, before.PendingChoices);
                await click('build_return');
                await page.waitForFunction(() => window.__touhouProbe.Screen === 'choices');
                await click('growth_choice_1');
                await page.waitForFunction(() => window.__touhouProbe.Phase === 'Playing');
                const after = await state();
                assert.equal(after.AllocatedPoints, before.AllocatedPoints + 1);
                assert.equal(after.PendingChoices, before.PendingChoices - 1);
                for (let index = 1; index < before.ChoiceIds.length; index++) {
                    if (before.ChoiceAbilities[index] !== before.ChoiceAbilities[0])
                        assert.ok(after.AvailableUpgradeIds.includes(before.ChoiceIds[index]), 'Other directions stay legal after investment');
                }
                entry.singleInputAndOtherDirectionsAvailable = true;
            });
        }
        for (const hero of ['reimu', 'marisa']) {
            await scenario('boss-' + hero, 'zh', false, 'boss-' + hero, async ({ page, state, entry }) => {
                const current = await state();
                assert.equal(current.BossCharacter.toLowerCase(), hero);
                assert.notEqual(current.BossCharacter, current.Hero);
                assert.equal(current.BossPhase, 2);
                if (hero === 'marisa') assert.ok(current.HostileStars > 0 && current.HostileStars <= 24);
                await page.screenshot({ path: path.join(output, 'boss-' + hero + '.png') });
                entry.state = current;
            });
        }
        for (const language of ['zh', 'en']) {
            await scenario('continuation-' + language, language, true, 'challenge-result', async ({ page, state, click, entry }) => {
                const before = await state();
                assert.equal(before.Screen, 'result');
                assert.equal(before.StandardVictory, true);
                assert.equal(before.EndlessRounds, 1);
                assert.equal(before.CanContinue, true);
                await page.screenshot({ path: path.join(output, 'continuation-' + language + '.png') });
                await click(language === 'zh' ? '继续挑战' : 'Continue challenge');
                await page.waitForFunction(() => window.__touhouProbe.Phase === 'Playing');
                assert.equal((await state()).StandardVictory, true);
                assert.equal((await state()).CompletedRuns, before.CompletedRuns);
                entry.continuedWithoutDuplicateVictory = true;
            });
        }
        report.passed = report.checks.length === 12 && report.checks.every(check => check.passed);
        assert.equal(report.passed, true);
        console.log('LONG_GROWTH_WEB_PASS', output);
    } finally {
        await browser.close();
        host.server.closeAllConnections();
        await new Promise(resolve => host.server.close(resolve));
        await fs.writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2) + '\n', 'utf8');
    }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
