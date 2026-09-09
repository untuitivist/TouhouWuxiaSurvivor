const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const childProcess = require('node:child_process');
const { chromium } = require('playwright');
const { startServer, texturePath } = require('./server.cjs');

async function bounded(promise, milliseconds = 120000) {
    let timer;
    try { return await Promise.race([promise, new Promise((resolve, reject) => { timer = setTimeout(() => reject(new Error('Experiment timed out')), milliseconds); })]); }
    finally { clearTimeout(timer); }
}

async function main() {
    const root = path.resolve(__dirname, '../..');
    const output = path.join(root, 'artifacts/webgpu-lab', new Date().toISOString().replace(/[:.]/g, '-'));
    await fs.mkdir(output, { recursive: true });
    const report = { scope: 'Local rendering-only experiment; not a Godot/C# game build or full-game FPS test.', output, physicalMobileTested: false, unsafeBrowserFlags: [], gameWebgpuEnabledByExperiment: false, sourceRevision: childProcess.execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(), inputHashes: {}, checks: [], passed: false };
    const runtimeArguments = ['diff', '--', 'game', 'assets', 'platform', 'project.godot', 'export_presets.cfg', 'TouhouWuxiaSurvivor.csproj', 'CHANGELOG.md'];
    const runtimeBefore = childProcess.execFileSync('git', runtimeArguments, { cwd: root, encoding: 'utf8', maxBuffer: 5000000 });
    for (const relative of ['tools/webgpu/scene.mjs', 'tools/webgpu/renderers.mjs', 'tools/webgpu/lab.mjs', 'tools/webgpu/index.html', 'game/presentation/SpriteBatch.cs', texturePath, 'project.godot']) report.inputHashes[relative] = crypto.createHash('sha256').update(await fs.readFile(path.join(root, relative))).digest('hex');
    const host = await startServer();
    let browser;
    try {
        browser = await chromium.launch({ executablePath: process.env.WEBGPU_BROWSER_EXE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true });
        report.browser = browser.version();
        async function scenario(name, action, { touch = false, fault = null } = {}) {
            const context = await browser.newContext({ viewport: touch ? { width: 844, height: 390 } : { width: 1280, height: 960 }, hasTouch: touch, isMobile: touch, deviceScaleFactor: touch ? 3 : 1 });
            const check = { name, touchEmulation: touch, injectedFault: fault, passed: false, errors: [], console: [] };
            report.checks.push(check);
            if (fault) await context.addInitScript(failure => {
                if (failure === 'api-missing' || failure === 'both-missing') Object.defineProperty(navigator, 'gpu', { configurable: true, value: undefined });
                if (failure === 'adapter-missing') Object.defineProperty(navigator.gpu, 'requestAdapter', { value: async () => null });
                if (failure === 'device-rejected') {
                    const original = navigator.gpu.requestAdapter.bind(navigator.gpu);
                    Object.defineProperty(navigator.gpu, 'requestAdapter', { value: async options => {
                        const adapter = await original(options);
                        if (adapter) Object.defineProperty(adapter, 'requestDevice', { value: async () => { throw new Error('Injected device rejection'); } });
                        return adapter;
                    } });
                }
                if (failure === 'both-missing') {
                    const original = HTMLCanvasElement.prototype.getContext;
                    HTMLCanvasElement.prototype.getContext = function(kind, ...args) { return kind === 'webgl2' ? null : original.call(this, kind, ...args); };
                }
            }, fault);
            const page = await context.newPage();
            page.on('pageerror', error => check.errors.push(String(error)));
            page.on('requestfailed', request => check.errors.push(request.url() + ': ' + request.failure()?.errorText));
            page.on('console', message => { check.console.push({ type: message.type(), text: message.text() }); if (message.type() === 'error') check.errors.push(message.text()); });
            try {
                await page.goto(host.origin, { waitUntil: 'domcontentloaded' });
                await page.waitForFunction(() => window.webgpuLab && ['ready', 'unavailable'].includes(window.webgpuLab.state().phase), undefined, { timeout: 120000 });
                check.initial = await page.evaluate(() => window.webgpuLab.state());
                assert.equal(check.initial.isolated, false);
                assert.equal(check.initial.sharedArrayBuffer, 'undefined');
                await action(page, check);
                assert.deepEqual(check.errors, []);
                check.passed = true;
                console.log('WEBGPU_LAB_CHECK_PASS', name);
            } catch (error) {
                check.error = String(error.stack || error);
                check.failureState = await page.evaluate(() => window.webgpuLab?.state()).catch(() => null);
                await page.screenshot({ path: path.join(output, name + '-failure.png'), fullPage: true }).catch(() => {});
                throw error;
            } finally { await context.close(); }
        }

        await scenario('real-backend-comparison', async (page, check) => {
            assert.equal(check.initial.phase, 'ready');
            report.webgpuAvailable = check.initial.backend === 'webgpu';
            report.hardwareAdapter = report.webgpuAvailable && check.initial.info.isFallbackAdapter === false;
            if (report.webgpuAvailable) {
                check.comparison = await bounded(page.evaluate(() => window.webgpuLab.compare()));
                assert.equal(check.comparison.results.length, 6);
                assert.ok(check.comparison.results.every(result => result.count === 1000 && result.bytesPerFrame === 64000 && result.drawCallsPerFrame === 1 && !result.gpuTimeMeasured && !result.fullGameFpsMeasured));
                await page.screenshot({ path: path.join(output, 'comparison.png'), fullPage: true });
                await page.evaluate(async () => { await window.webgpuLab.select('webgpu'); window.canvasBeforeDeviceLoss = document.querySelector('canvas'); window.webgpuLab.forceDeviceLoss(); });
                await page.waitForFunction(() => window.webgpuLab.state().phase === 'ready' && window.webgpuLab.state().backend === 'webgl2' && window.webgpuLab.state().fallback);
                check.deviceLossRecovery = await page.evaluate(() => ({ state: window.webgpuLab.state(), canvasReplaced: window.canvasBeforeDeviceLoss !== document.querySelector('canvas') }));
                assert.equal(check.deviceLossRecovery.canvasReplaced, true);
                assert.match(check.deviceLossRecovery.state.recoveryReason, /WebGPU device lost/);
                await page.screenshot({ path: path.join(output, 'device-loss-fallback.png'), fullPage: true });
                check.recoveredMeasurement = await bounded(page.evaluate(() => window.webgpuLab.measure('webgl2', 60)));
            } else {
                check.webgpuMeasurementSkipped = 'No usable WebGPU backend; do not report WebGL results as WebGPU.';
                check.fallbackMeasurement = await bounded(page.evaluate(() => window.webgpuLab.measure('webgl2', 60)));
            }
        });

        for (const fault of ['api-missing', 'adapter-missing', 'device-rejected', 'both-missing']) {
            if (!report.webgpuAvailable && ['adapter-missing', 'device-rejected'].includes(fault)) continue;
            await scenario(fault, async (page, check) => {
                assert.equal(check.initial.phase, fault === 'both-missing' ? 'unavailable' : 'ready');
                assert.equal(check.initial.backend, fault === 'both-missing' ? null : 'webgl2');
                assert.equal(check.initial.attempts.length, 2);
                assert.equal(check.initial.attempts[0].available, false);
                await page.screenshot({ path: path.join(output, fault + '.png'), fullPage: true });
                if (fault !== 'both-missing') {
                    assert.equal(check.initial.fallback, true);
                    check.actualDraw = await bounded(page.evaluate(() => window.webgpuLab.measure('webgl2', 60)));
                } else assert.equal(await page.locator('canvas').count(), 0);
            }, { fault });
        }
        await scenario('touch-dpr3', async (page, check) => {
            assert.equal(check.initial.phase, 'ready');
            const canvas = await page.locator('canvas').evaluate(element => ({ width: element.width, height: element.height }));
            assert.deepEqual(canvas, { width: 1280, height: 720 });
            await page.locator('#gl').tap();
            await page.waitForFunction(() => window.webgpuLab.state().backend === 'webgl2' && window.webgpuLab.state().phase === 'ready');
            check.final = await page.evaluate(() => window.webgpuLab.state());
            await page.screenshot({ path: path.join(output, 'touch-dpr3.png'), fullPage: true });
        }, { touch: true });
        const runtimeAfter = childProcess.execFileSync('git', runtimeArguments, { cwd: root, encoding: 'utf8', maxBuffer: 5000000 });
        assert.equal(runtimeAfter, runtimeBefore);
        report.runtimeDiffUnchanged = true;
        report.passed = true;
    } catch (error) { report.error = String(error.stack || error); throw error; }
    finally {
        await browser?.close();
        await host.close();
        await fs.writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2) + '\n', 'utf8');
        await fs.writeFile(path.join(root, 'artifacts/webgpu-lab/latest.json'), JSON.stringify({ output, report: path.join(output, 'report.json') }, null, 2) + '\n', 'utf8');
    }
    console.log('WEBGPU_LAB_PASS', JSON.stringify({ output, webgpuAvailable: report.webgpuAvailable, hardwareAdapter: report.hardwareAdapter, gameBackendChanged: false }));
}
main().catch(error => { console.error(error); process.exitCode = 1; });
