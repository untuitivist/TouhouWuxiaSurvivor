const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');
const { startProbeServer } = require('../web_probe/server.cjs');

async function main() {
    const root = path.resolve(__dirname, '../..');
    const build = JSON.parse(await fs.readFile(path.join(root, 'artifacts/web-compatible-latest.json'), 'utf8'));
    const output = path.join(build.build, 'scenery-verification', new Date().toISOString().replace(/[:.]/g, '-'));
    await fs.mkdir(output, { recursive: true });
    const report = { build: build.build, physicalDeviceTested: false, checks: [] };
    const browser = await chromium.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true });
    const scenes = [
        { name: 'title-desktop', width: 1280, height: 720 },
        { name: 'title-small', width: 640, height: 360 },
        { name: 'title-touch', width: 844, height: 390, touch: true },
        { name: 'scenery-reimu', width: 1280, height: 720, fixture: 'reimu-field' },
        { name: 'scenery-marisa', width: 1280, height: 720, fixture: 'marisa-beam' }
    ];
    try {
        for (const scene of scenes) {
            const args = ['--', '--web-validation', ...(scene.fixture ? ['--web-fixture=' + scene.fixture] : [])];
            const host = await startProbeServer(build.site, 0, { isolation: false, entryArguments: args });
            const context = await browser.newContext({ viewport: { width: scene.width, height: scene.height }, hasTouch: Boolean(scene.touch), isMobile: Boolean(scene.touch) });
            const check = { name: scene.name, errors: [], requests: [] };
            report.checks.push(check);
            try {
                const page = await context.newPage();
                page.on('console', message => { if (message.type() === 'error') check.errors.push(message.text()); });
                page.on('pageerror', error => check.errors.push(String(error)));
                page.on('requestfailed', request => check.requests.push(request.url() + ' ' + request.failure()?.errorText));
                await page.goto(host.origin + '/TouhouSurvivor/', { waitUntil: 'domcontentloaded' });
                await page.waitForFunction(expected => window.__touhouProbe?.Screen === expected && !document.querySelector('#loading'), scene.fixture ? 'playing' : 'title', { timeout: 120000 });
                await page.waitForTimeout(500);
                check.state = await page.evaluate(() => ({ ...window.__touhouProbe, isolated: crossOriginIsolated, sharedArrayBuffer: typeof SharedArrayBuffer }));
                assert.equal(check.state.isolated, false);
                assert.equal(check.state.sharedArrayBuffer, 'undefined');
                assert.equal(check.state.RenderWidth, 1280);
                assert.equal(check.state.RenderHeight, 720);
                assert.equal(check.state.Warning, '');
                if (scene.fixture) {
                    assert.equal(check.state.Tick, 45);
                    assert.ok(check.state.BatchInstances > 0);
                    assert.equal(check.state.Hero, scene.fixture.startsWith('reimu') ? 'Reimu' : 'Marisa');
                } else {
                    assert.ok(check.state.Controls.some(control => control.Text.includes('踏入夜境')));
                }
                assert.deepEqual(check.errors, []);
                assert.deepEqual(check.requests, []);
                await page.screenshot({ path: path.join(output, scene.name + '.png') });
                check.passed = true;
                console.log('WEB_SCENERY_PASS', scene.name);
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
    await fs.writeFile(path.join(build.build, 'scenery-verification-latest.json'), JSON.stringify({ report: path.join(output, 'report.json') }) + '\n', 'utf8');
    console.log('WEB_SCENERY_VALIDATION_PASS', output);
}

main().catch(error => { console.error(error); process.exitCode = 1; });
