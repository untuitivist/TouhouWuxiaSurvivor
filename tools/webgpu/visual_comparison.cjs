const fs = require('node:fs/promises');
const path = require('node:path');
const assert = require('node:assert/strict');
const { chromium } = require('playwright');
const sharp = require('sharp');
const { startProbeServer } = require('../web_probe/server.cjs');
const { attachRendererEvidence, assertRendererEvidence } = require('./browser_evidence.cjs');

const root = path.resolve(__dirname, '../..');
const option = (name, fallback) => process.argv.find(argument => argument.startsWith('--' + name + '='))?.slice(name.length + 3) ?? fallback;
const output = path.resolve(root, option('output', path.join('artifacts/webgpu-visuals', new Date().toISOString().replace(/[:.]/g, '-'))));
const fixtures = ['performance', 'reimu-field', 'reimu-spell', 'marisa-stars', 'marisa-warmup', 'marisa-beam'];
const report = { output, physicalMobileTested: false, scope: 'Actual unchanged C# static render fixtures. Browser compositor captures supplement, not replace, engine readback/color-state gates.', tolerances: { channel: 8, mismatchRatio: 0.02, meanAbsoluteRgb: 2 }, captures: [], comparisons: [], passed: false };

async function capture(backend, fixture) {
    const build = JSON.parse(await fs.readFile(path.join(root, backend === 'webgpu' ? 'artifacts/web-webgpu-latest.json' : 'artifacts/web-compatible-latest.json'), 'utf8'));
    const args = ['--', '--web-validation', '--web-fixture=' + fixture, '--web-language=zh'];
    const host = await startProbeServer(build.site, 0, { isolation: false, entryArguments: args });
    let browser;
    const record = { backend, fixture, build: build.build, errors: [], image: path.join(output, backend + '-' + fixture + '.png'), passed: false };
    report.captures.push(record);
    try {
        browser = await chromium.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true });
        const context = await browser.newContext({ viewport: { width: 1280, height: 720 }, deviceScaleFactor: 1 });
        await attachRendererEvidence(context);
        const page = await context.newPage();
        page.on('pageerror', error => record.errors.push(String(error)));
        page.on('console', message => { if (message.type() === 'error') record.errors.push(message.text()); });
        page.on('requestfailed', request => record.errors.push(request.url() + ': ' + request.failure()?.errorText));
        await page.goto(host.origin + '/TouhouSurvivor/', { waitUntil: 'domcontentloaded' });
        await page.waitForFunction(() => window.__touhouProbe?.Phase === 'Playing' && !document.getElementById('loading'), undefined, { timeout: 90000 });
        const firstTick = await page.evaluate(() => window.__touhouProbe.Tick);
        await page.waitForTimeout(1000);
        record.state = await page.evaluate(() => window.__touhouProbe);
        record.renderer = await page.evaluate(() => window.__actualGpuEvidence);
        assertRendererEvidence(record.renderer, backend);
        assert.equal(record.state.Tick, firstTick, 'Visual fixtures must not progress hidden simulation');
        assert.equal(record.state.Hero, fixture.startsWith('marisa') ? 'Marisa' : 'Reimu');
        assert.deepEqual(record.errors, []);
        await page.locator('#canvas').screenshot({ path: record.image, timeout: 10000 });
        const image = await sharp(record.image).removeAlpha().raw().toBuffer();
        let nonBlack = 0;
        const colors = new Set();
        for (let index = 0; index < image.length; index += 3) {
            if (image[index] + image[index + 1] + image[index + 2] > 24) nonBlack++;
            colors.add((image[index] << 16) | (image[index + 1] << 8) | image[index + 2]);
        }
        record.nonBlackRatio = nonBlack / (image.length / 3);
        record.colors = colors.size;
        assert.ok(record.nonBlackRatio > 0.5 && record.colors > 32, 'Both blank frames must not pass comparison');
        record.passed = true;
    } catch (error) { record.failure = String(error.stack || error); }
    finally {
        if (browser) await browser.close();
        host.server.closeAllConnections();
        await new Promise(resolve => host.server.close(resolve));
    }
    console.log('ACTUAL_VISUAL_CAPTURE', backend, fixture, record.passed, record.failure || '');
}

async function compare(fixture) {
    const pair = report.captures.filter(record => record.fixture === fixture);
    if (!pair.every(record => record.passed)) return;
    const gameplayFields = ['Hero', 'Phase', 'Tick', 'Time', 'X', 'Y', 'Enemies', 'Projectiles', 'Pickups', 'BatchInstances', 'RenderWidth', 'RenderHeight'];
    for (const key of gameplayFields) assert.equal(pair[0].state[key], pair[1].state[key], 'Fixture state must match before comparing pixels: ' + key);
    const images = await Promise.all(pair.map(record => sharp(record.image).ensureAlpha().raw().toBuffer({ resolveWithObject: true })));
    assert.deepEqual(images[0].info, images[1].info);
    const pixels = images[0].info.width * images[0].info.height;
    const difference = Buffer.alloc(pixels * 4);
    let totalError = 0;
    let mismatches = 0;
    for (let index = 0; index < pixels; index++) {
        let largest = 0;
        for (let channel = 0; channel < 3; channel++) {
            const error = Math.abs(images[0].data[index * 4 + channel] - images[1].data[index * 4 + channel]);
            totalError += error;
            largest = Math.max(largest, error);
        }
        if (largest > report.tolerances.channel) mismatches++;
        difference[index * 4] = Math.min(255, largest * 4);
        difference[index * 4 + 3] = 255;
    }
    const result = { fixture, pixels, mismatchRatio: mismatches / pixels, meanAbsoluteRgb: totalError / (pixels * 3), difference: path.join(output, 'difference-' + fixture + '.png') };
    result.passed = result.mismatchRatio <= report.tolerances.mismatchRatio && result.meanAbsoluteRgb <= report.tolerances.meanAbsoluteRgb;
    await sharp(difference, { raw: { width: images[0].info.width, height: images[0].info.height, channels: 4 } }).png().toFile(result.difference);
    report.comparisons.push(result);
    console.log('ACTUAL_VISUAL_COMPARISON', JSON.stringify(result));
}

async function main() {
    const existing = await fs.readdir(output).catch(error => { if (error.code === 'ENOENT') return []; throw error; });
    assert.equal(existing.length, 0, 'Existing comparison output must be preserved; use a new directory');
    await fs.mkdir(output, { recursive: true });
    try {
        for (const fixture of fixtures) {
            for (const backend of ['webgl2', 'webgpu']) await capture(backend, fixture);
            await compare(fixture);
            await fs.writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2), 'utf8');
        }
        report.passed = report.captures.length === fixtures.length * 2 && report.captures.every(record => record.passed) && report.comparisons.length === fixtures.length && report.comparisons.every(record => record.passed);
    } finally { await fs.writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2), 'utf8'); }
    if (!report.passed) process.exitCode = 1;
    console.log('ACTUAL_VISUAL_RESULT', output, report.passed);
}

main().catch(error => { console.error(error); process.exitCode = 1; });
