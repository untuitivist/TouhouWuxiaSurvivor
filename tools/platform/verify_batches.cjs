const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');
const { startProbeServer } = require('../web_probe/server.cjs');
const { installBatchColorStress } = require('./batch_color_state.cjs');

async function main({ colorStateStress = false } = {}) {
    const stress = colorStateStress || process.argv.includes('--color-state-stress');
    const root = path.resolve(__dirname, '../..');
    const build = JSON.parse(await fs.readFile(path.join(root, 'artifacts/web-compatible-latest.json'), 'utf8'));
    const output = path.join(build.build, 'batch-verification', new Date().toISOString().replace(/[:.]/g, '-'));
    await fs.mkdir(output, { recursive: true });
    const browser = await chromium.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true });
    const report = { build: build.build, browser: browser.version(), physicalDeviceTested: false, injectedDefaultColor: stress, checks: [] };
    try {
        for (const hero of ['reimu', 'marisa']) {
            const args = ['--', '--rebirth-batch-smoke'];
            if (hero === 'marisa') args.push('--rebirth-batch-marisa');
            const host = await startProbeServer(build.site, 0, { isolation: false, entryArguments: args });
            const context = await browser.newContext({ viewport: { width: 1280, height: 720 } });
            const check = { hero, events: [], errors: [], failedRequests: [] };
            report.checks.push(check);
            try {
                if (stress) await context.addInitScript(installBatchColorStress);
                const page = await context.newPage();
                page.on('console', message => {
                    check.events.push(message.text());
                    if (message.type() === 'error') check.errors.push(message.text());
                });
                page.on('pageerror', error => check.errors.push(String(error)));
                page.on('requestfailed', request => check.failedRequests.push({ url: request.url(), error: request.failure()?.errorText }));
                await page.goto(host.origin + '/TouhouSurvivor/', { waitUntil: 'domcontentloaded' });
                await page.waitForFunction(() => document.querySelector('#loading') === null, undefined, { timeout: 120000 });
                check.capabilities = await page.evaluate(() => ({ isolated: crossOriginIsolated, sharedArrayBuffer: typeof SharedArrayBuffer }));
                assert.deepEqual(check.capabilities, { isolated: false, sharedArrayBuffer: 'undefined' });
                const deadline = Date.now() + 180000;
                while (!check.events.some(text => /SPRITE_BATCH_VISUAL_PASS|SPRITE_BATCH_VISUAL_FAIL/.test(text)) && !check.errors.length && Date.now() < deadline) await page.waitForTimeout(500);
                check.colorState = await page.evaluate(() => globalThis.batchColorStress ?? null);
                await page.screenshot({ path: path.join(output, hero + '.png') });
                assert.deepEqual(check.errors, []);
                assert.deepEqual(check.failedRequests, []);
                assert.ok(check.events.some(text => /SPRITE_BATCH_VISUAL_PASS.+checks=72/.test(text)), check.events.slice(-5).join('\n'));
                assert.equal(check.events.filter(text => text.startsWith('BATTLE_BATCH_CHECK')).length, 12);
                assert.equal(check.events.filter(text => text.startsWith('BATCH_COLOR_CHECK')).length, 24);
                assert.ok(check.events.includes('BATCH_COLOR_VISUAL_PASS checks=24'));
                if (stress) {
                    assert.ok(check.colorState?.draws > 24, 'Fault injection must exercise actual instanced draws');
                    assert.equal(check.colorState.missingColor, 0, 'Instanced draws must supply their vertex colors');
                }
                check.passed = true;
                console.log('WEB_BATCH_RENDER_PASS', hero);
            } finally {
                await context.close();
                host.server.closeAllConnections();
                await new Promise(resolve => host.server.close(resolve));
            }
        }
        report.passed = true;
    } finally {
        await browser.close();
        await fs.writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2) + '\n', 'utf8');
    }
    await fs.writeFile(path.join(build.build, stress ? 'batch-color-stress-latest.json' : 'batch-verification-latest.json'), JSON.stringify({ build: build.build, report: path.join(output, 'report.json') }) + '\n', 'utf8');
    console.log('WEB_BATCH_VALIDATION_PASS', output);
}

module.exports = { verifyBatches: main };
if (require.main === module) main().catch(error => { console.error(error); process.exitCode = 1; });
