const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');
const { startProbeServer } = require('../web_probe/server.cjs');

async function main() {
    const root = path.resolve(__dirname, '../..');
    const build = JSON.parse(await fs.readFile(path.join(root, 'artifacts/web-compatible-latest.json'), 'utf8'));
    const output = path.join(build.build, 'hero-art-verification', new Date().toISOString().replace(/[:.]/g, '-'));
    await fs.mkdir(output, { recursive: true });
    const report = { build: build.build, physicalDeviceTested: false, checks: [] };
    const browser = await chromium.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true });
    try {
        for (const [mode, tick] of [['reimu-field', 45], ['reimu-spell', 14], ['marisa-stars', 18], ['marisa-warmup', 8], ['marisa-beam', 45]]) {
            const host = await startProbeServer(build.site, 0, { isolation: false, entryArguments: ['--', '--web-validation', '--web-fixture=' + mode] });
            const context = await browser.newContext({ viewport: { width: 1280, height: 720 } });
            const check = { mode, errors: [], requests: [] };
            report.checks.push(check);
            try {
                const page = await context.newPage();
                page.on('console', message => { if (message.type() === 'error') check.errors.push(message.text()); });
                page.on('pageerror', error => check.errors.push(String(error)));
                page.on('requestfailed', request => check.requests.push(request.url() + ' ' + request.failure()?.errorText));
                await page.goto(host.origin + '/TouhouSurvivor/', { waitUntil: 'domcontentloaded' });
                await page.waitForFunction(expected => window.__touhouProbe?.Tick === expected && !document.querySelector('#loading'), tick, { timeout: 120000 });
                await page.waitForTimeout(500);
                check.state = await page.evaluate(() => ({ ...window.__touhouProbe, isolated: crossOriginIsolated, sharedArrayBuffer: typeof SharedArrayBuffer }));
                assert.equal(check.state.Tick, tick);
                assert.equal(check.state.Hero, mode.startsWith('reimu') ? 'Reimu' : 'Marisa');
                assert.equal(check.state.isolated, false);
                assert.equal(check.state.sharedArrayBuffer, 'undefined');
                assert.equal(check.state.RenderWidth, 1280);
                assert.equal(check.state.RenderHeight, 720);
                assert.ok(check.state.BatchInstances > 0);
                assert.equal(check.state.Warning, '');
                assert.deepEqual(check.errors, []);
                assert.deepEqual(check.requests, []);
                await page.screenshot({ path: path.join(output, mode + '.png') });
                check.passed = true;
                console.log('WEB_HERO_ART_PASS', mode);
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
    await fs.writeFile(path.join(build.build, 'hero-art-verification-latest.json'), JSON.stringify({ report: path.join(output, 'report.json') }) + '\n', 'utf8');
    console.log('WEB_HERO_ART_VALIDATION_PASS', output);
}

main().catch(error => { console.error(error); process.exitCode = 1; });
