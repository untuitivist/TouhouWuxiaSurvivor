const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const { chromium } = require('playwright');
const { startProbeServer } = require('../web_probe/server.cjs');
const { verificationBuildPath, verificationOutputRoot } = require('./verification_paths.cjs');

async function verifyRawLoading() {
    const root = path.resolve(__dirname, '../..');
    const build = JSON.parse(await fs.readFile(verificationBuildPath(root, 'artifacts/web-compatible-latest.json'), 'utf8'));
    const output = verificationOutputRoot(build);
    await fs.mkdir(output, { recursive: true });
    const pack = await fs.readFile(path.join(build.site, 'TouhouSurvivor/index.pck'));
    const expected = crypto.createHash('sha256').update(pack).digest('hex');
    const loader = await fs.readFile(path.join(build.site, 'TouhouSurvivor/index.loader.js'), 'utf8');
    const entryHtml = '<!doctype html><html><body>' + ['loading', 'notice', 'progress', 'amount', 'speed', 'remaining', 'percentage', 'phase', 'download-total', 'download-note', 'retry'].map(name => '<div id="' + name + '"></div>').join('') + '</body></html>';
    const host = await startProbeServer(build.site, 0, { isolation: false, entryHtml });
    const browser = await chromium.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true });
    const report = { build: build.build, physicalDeviceTested: false, checks: [] };
    try {
        for (let attempt = 0; attempt < 20; attempt++) {
            const context = await browser.newContext();
            const page = await context.newPage();
            const check = { attempt, failedRequests: [], errors: [] };
            try {
                await page.goto(host.origin + '/TouhouSurvivor/');
                await page.addScriptTag({ content: loader });
                page.on('requestfailed', request => check.failedRequests.push({ url: request.url(), error: request.failure()?.errorText }));
                page.on('pageerror', error => check.errors.push(String(error)));
                const before = host.requests.filter(request => request.path.endsWith('/index.pck')).length;
                check.result = await page.evaluate(async bytes => {
                    let result;
                    const originalFetch = fetch;
                    window.Engine = class {
                        static getMissingFeatures() { return []; }
                        async startGame() {
                            const response = await fetch('/TouhouSurvivor/index.pck');
                            const buffer = await response.arrayBuffer();
                            const digest = [...new Uint8Array(await crypto.subtle.digest('SHA-256', buffer))].map(value => value.toString(16).padStart(2, '0')).join('');
                            result = { bytes: buffer.byteLength, digest };
                        }
                    };
                    const loading = TouhouLoading.create({ fileSizes: { '/TouhouSurvivor/index.pck': bytes } }, null, false);
                    await loading.start();
                    return { ...result, ready: document.body.dataset.gameReady, restored: fetch === originalFetch, isolated: crossOriginIsolated, sharedArrayBuffer: typeof SharedArrayBuffer };
                }, pack.length);
                await page.waitForTimeout(100);
                assert.equal(check.result.bytes, pack.length);
                assert.equal(check.result.digest, expected);
                assert.equal(check.result.ready, 'true');
                assert.equal(check.result.restored, true);
                assert.equal(check.result.isolated, false);
                assert.equal(check.result.sharedArrayBuffer, 'undefined');
                assert.equal(host.requests.filter(request => request.path.endsWith('/index.pck')).length - before, 1);
                assert.deepEqual(check.failedRequests, []);
                assert.deepEqual(check.errors, []);
                check.passed = true;
                console.log('RAW_LOADING_PASS', attempt + 1);
            } finally {
                report.checks.push(check);
                await context.close();
            }
        }
        report.passed = true;
    } finally {
        await fs.writeFile(path.join(output, 'raw-loading-verification.json'), JSON.stringify(report, null, 2), 'utf8');
        await browser.close();
        host.server.closeAllConnections();
        await new Promise(resolve => host.server.close(resolve));
    }
}

if (require.main === module) verifyRawLoading().catch(error => { console.error(error); process.exitCode = 1; });
module.exports = { verifyRawLoading };
