const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const assert = require('node:assert/strict');
const { chromium } = require('playwright');
const { startProbeServer } = require('../web_probe/server.cjs');
const { attachRendererEvidence, assertRendererEvidence } = require('./browser_evidence.cjs');

const option = (name, fallback) => process.argv.find(argument => argument.startsWith('--' + name + '='))?.slice(name.length + 3) ?? fallback;
const bounded = async (promise, milliseconds = 10000) => {
    let timer;
    try { return await Promise.race([promise, new Promise((resolve, reject) => { timer = setTimeout(() => reject(new Error('Bounded browser operation timed out')), milliseconds); })]); }
    finally { clearTimeout(timer); }
};

async function main() {
    const root = path.resolve(__dirname, '../..');
    const buildPointer = path.resolve(root, option('build', 'artifacts/web-webgpu-latest.json'));
    const build = JSON.parse(await fs.readFile(buildPointer, 'utf8'));
    const manifest = JSON.parse(await fs.readFile(build.manifest, 'utf8'));
    const expected = option('expected', 'webgpu');
    assert.ok(['webgpu', 'webgl2'].includes(expected));
    const fixture = option('fixture', 'title');
    const hero = option('hero', 'reimu');
    const fault = option('fault', 'none');
    assert.ok(['reimu', 'marisa'].includes(hero));
    assert.ok(['none', 'missing-api', 'no-adapter', 'device-rejected', 'device-loss'].includes(fault));
    if (fault === 'device-loss') assert.equal(fixture, 'combat-performance');
    const output = path.resolve(root, option('output', path.join('artifacts/webgpu-game', new Date().toISOString().replace(/[:.]/g, '-'))));
    const existing = await fs.readdir(output).catch(error => { if (error.code === 'ENOENT') return []; throw error; });
    assert.equal(existing.length, 0, 'Existing probe output must be preserved; use a new directory');
    await fs.mkdir(output, { recursive: true });
    const report = { output, build, expected, fixture, hero, fault, templateSha256: manifest.templateSha256, physicalMobileTested: false, unsafeBrowserFlags: [], events: [], eventCount: 0, passed: false };
    const args = ['--', '--web-validation', '--web-language=zh'];
    if (fixture !== 'title') args.push('--web-fixture=' + fixture, '--web-load=320');
    if (hero === 'marisa') args.push('--web-marisa');
    const host = await startProbeServer(build.site, crypto.randomInt(49152, 64000), { isolation: false, entryArguments: args });
    let browser;
    let page;
    try {
        browser = await chromium.launch({ executablePath: process.env.WEBGPU_BROWSER_EXE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true });
        report.browser = browser.version();
        const context = await browser.newContext({ viewport: { width: 1280, height: 720 } });
        await attachRendererEvidence(context, { fault });
        page = await context.newPage();
        const record = (type, text) => { report.eventCount++; if (report.events.length < 300) report.events.push({ type, text }); };
        page.on('console', message => record(message.type(), message.text()));
        page.on('pageerror', error => record('pageerror', String(error)));
        page.on('requestfailed', request => record('requestfailed', request.url() + ': ' + request.failure()?.errorText));
        await page.goto(host.origin + '/TouhouSurvivor/', { waitUntil: 'domcontentloaded' });
        await page.waitForFunction(() => window.__touhouProbe && !document.getElementById('loading'), undefined, { timeout: fault !== 'none' && fault !== 'device-loss' ? 30000 : 90000 });
        await page.waitForTimeout(3000);
        report.initial = await bounded(page.evaluate(() => ({ state: window.__touhouProbe, gpu: window.__actualGpuEvidence, isolated: crossOriginIsolated, sharedMemory: typeof SharedArrayBuffer })));
        await page.screenshot({ path: path.join(output, 'game.png'), timeout: 10000 });
        assert.equal(report.initial.isolated, false);
        assert.equal(report.initial.sharedMemory, 'undefined');
        assertRendererEvidence(report.initial.gpu, fault !== 'none' && fault !== 'device-loss' ? 'webgl2' : expected);
        if (fault !== 'none' && fault !== 'device-loss') assert.equal(report.initial.gpu.faultInjected, true);
        assert.ok(report.initial.state.HasChineseGlyphs);
        if (fixture === 'title') assert.equal(report.initial.state.Screen, 'title');
        else {
            assert.equal(report.initial.state.Phase, 'Playing');
            assert.ok(report.initial.state.Tick > 30);
            assert.equal(report.initial.state.Hero, hero === 'marisa' ? 'Marisa' : 'Reimu');
        }
        assert.deepEqual(report.initial.gpu.errors, []);
        assert.ok(!report.events.some(event => ['error', 'pageerror', 'requestfailed'].includes(event.type)), 'Actual game emitted an error; inspect the retained report');
        if (fault === 'device-loss') {
            await page.evaluate(() => {
                if (!window.__actualGpuDevice) throw new Error('No actual GPU device to destroy');
                window.__actualGpuEvidence.faultInjected = true;
                window.__actualGpuDevice.destroy();
            });
            await page.waitForTimeout(10000);
            report.afterLoss = await bounded(page.evaluate(() => ({ state: window.__touhouProbe, gpu: window.__actualGpuEvidence })));
            await page.screenshot({ path: path.join(output, 'after-loss.png'), timeout: 10000 });
            assert.ok(report.afterLoss.gpu.lost.some(loss => loss.reason === 'destroyed'), 'The actual engine device must have been lost');
            assertRendererEvidence(report.afterLoss.gpu, 'webgl2', { allowDeviceLoss: true });
            assert.equal(report.afterLoss.state.Phase, 'Playing');
            assert.equal(report.afterLoss.state.Hero, report.initial.state.Hero);
            assert.ok(report.afterLoss.state.Tick > report.initial.state.Tick, 'Recovery must preserve the active run and resume simulation');
        }
        report.passed = true;
    } catch (error) {
        report.failure = String(error.stack || error);
        if (page) {
            report.failureState = await bounded(page.evaluate(() => ({ state: window.__touhouProbe, gpu: window.__actualGpuEvidence, loading: document.getElementById('loading')?.textContent })), 5000).catch(error => ({ captureError: String(error) }));
            await page.screenshot({ path: path.join(output, 'failure.png'), timeout: 5000 }).catch(() => {});
        }
        process.exitCode = 1;
    } finally {
        try { if (browser) await bounded(browser.close()); }
        catch (error) { report.passed = false; report.teardownFailure = String(error); process.exitCode = 1; }
        finally {
            host.server.closeAllConnections();
            await new Promise(resolve => host.server.close(resolve));
            await fs.writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2), 'utf8');
            await fs.writeFile(path.join(root, 'artifacts/webgpu-game/latest.json'), JSON.stringify({ output, report: path.join(output, 'report.json') }, null, 2), 'utf8');
        }
    }
    console.log('ACTUAL_GAME_PROBE', JSON.stringify({ passed: report.passed, fixture, hero, expected, output, failure: report.failure }));
}

main().catch(error => { console.error(error); process.exitCode = 1; });
