const assert = require("node:assert/strict");
const fs = require("node:fs/promises");
const path = require("node:path");
const { chromium } = require("playwright");
const { startDeploymentFixture } = require("./deployment_fixture.cjs");
const { attachRendererEvidence, assertRendererEvidence } = require("../webgpu/browser_evidence.cjs");
const bounded = async (operation, milliseconds = 10000) => {
    let timer;
    try { return await Promise.race([operation, new Promise((resolve, reject) => { timer = setTimeout(() => reject(new Error("Browser operation did not complete within " + milliseconds + " ms")), milliseconds); })]); }
    finally { clearTimeout(timer); }
};

const option = (name, fallback) => process.argv.find(value => value.startsWith(`--${name}=`))?.slice(name.length + 3) ?? fallback;
const summarize = values => {
    const sorted = [...values].sort((left, right) => left - right);
    assert.ok(values.length > 0 && values.every(Number.isFinite));
    return { mean: values.reduce((total, value) => total + value, 0) / values.length, minimum: sorted[0], maximum: sorted.at(-1), p95: sorted[Math.floor(sorted.length * 0.95)] };
};

async function main() {
    const buildPath = path.resolve(option("build", "artifacts/web-compatible-latest.json"));
    const build = JSON.parse(await fs.readFile(buildPath, "utf8"));
    const output = path.resolve(option("output", path.join(build.build, "combat-benchmark.json")));
    const throttle = Number(option("throttle", "1"));
    const language = option("language", "zh");
    const hero = option("hero", "reimu");
    const expected = option("expected", "webgl2");
    const seconds = Number(option("seconds", "15"));
    assert.ok(["reimu", "marisa"].includes(hero));
    assert.ok(["webgpu", "webgl2"].includes(expected));
    assert.ok(Number.isFinite(seconds) && seconds >= 15 && seconds <= 600);
    assert.ok(["zh", "en"].includes(language));
    const enforce = process.argv.includes("--enforce");
    if (enforce) assert.equal(throttle, 1, "Release budgets use the unthrottled desktop reference environment");
    assert.ok(Number.isFinite(throttle) && throttle >= 1);
    const report = { buildPath, build, throttle, isolation: option("isolation", "false") === "true", physicalDeviceTested: false, scope: "Desktop Edge, real fixed-step combat with compatible growth branches and replenished entities; CPU emulation is not phone FPS.", scenarios: [] };
    report.language = language;
    Object.assign(report, { hero, expected, seconds, unsafeBrowserFlags: [] });
    report.gatesEnforced = enforce;
    report.passed = false;
    report.failures = [];
    const host = await startDeploymentFixture(build, { isolation: report.isolation });
    const browser = await chromium.launch({ executablePath: "C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe", headless: true });
    report.browser = browser.version();
    try {
        for (const load of option("loads", "40,180,320").split(",").map(Number)) {
            const context = await browser.newContext({ viewport: { width: 844, height: 390 }, hasTouch: true, isMobile: true, deviceScaleFactor: 1 });
            await attachRendererEvidence(context);
            try {
                const page = await context.newPage();
                const errors = [];
                page.on("pageerror", error => errors.push(String(error)));
                page.on("console", message => { if (message.type() === "error") errors.push(message.text()); });
                page.on("requestfailed", request => errors.push(request.url() + ": " + request.failure()?.errorText));
                await page.route("**/TouhouSurvivor/", async route => {
                    const response = await route.fetch();
                    const html = await response.text();
                    assert.ok(html.includes('"args":[]'));
                    const arguments_ = ["--", "--web-validation", "--web-fixture=combat-performance", "--web-load=" + load, "--web-language=" + language];
                    if (hero === "marisa") arguments_.push("--web-marisa");
                    await route.fulfill({ response, body: html.replace('"args":[]', '"args":' + JSON.stringify(arguments_)) });
                });
                await page.goto(host.origin + "/TouhouSurvivor/", { waitUntil: "domcontentloaded" });
                await page.waitForFunction(() => window.__touhouProbe?.Tick > 60 && !document.getElementById("loading"), {}, { timeout: 120000 });
                const capabilities = await bounded(page.evaluate(() => ({ isolated: crossOriginIsolated, sharedMemory: typeof SharedArrayBuffer })));
                assert.equal(capabilities.isolated, report.isolation);
                if (!report.isolation) assert.equal(capabilities.sharedMemory, "undefined");
                const session = await bounded(context.newCDPSession(page));
                await bounded(session.send("Emulation.setCPUThrottlingRate", { rate: throttle }));
                await page.waitForTimeout(3000);
                const samples = await bounded(page.evaluate(seconds => new Promise(resolve => {
                    const states = [];
                    const interval = setInterval(() => states.push({ ...window.__touhouProbe, wall: performance.now() }), 100);
                    setTimeout(() => { clearInterval(interval); resolve(states); }, seconds * 1000);
                }), seconds), seconds * 1000 + 15000);
                assert.ok(samples.length > 0);
                const first = samples[0];
                const last = samples.at(-1);
                console.log("SYSTEM_TIMINGS", JSON.stringify(["encounters", "enemies", "grid", "weapons", "projectiles", "progression"].map((name, index) => ({ name, ...summarize(samples.map(sample => sample.SystemMilliseconds[index])) }))));
                const result = { load, simulatedTicksPerSecond: (last.Tick - first.Tick) / ((last.wall - first.wall) / 1000), metrics: Object.fromEntries(["Fps", "SimulationMilliseconds", "EventMilliseconds", "BatchMilliseconds", "DrawCalls", "BatchInstances"].map(key => [key, summarize(samples.map(sample => sample[key]))])), errors, samples };
                assert.ok(last.Tick > first.Tick);
                assert.deepEqual(errors, []);
                report.scenarios.push(result);
                result.capabilities = capabilities;
                result.renderer = await bounded(page.evaluate(() => window.__actualGpuEvidence));
                assertRendererEvidence(result.renderer, expected);
                assert.ok(samples.every(sample => sample.Language === language));
                assert.ok(samples.every(sample => sample.Hero === (hero === "marisa" ? "Marisa" : "Reimu")));
                result.population = Object.fromEntries(["Enemies", "Projectiles", "Pickups", "BatchInstances"].filter(key => samples.every(sample => Number.isFinite(sample[key]))).map(key => [key, summarize(samples.map(sample => sample[key]))]));
                console.log("COMBAT_BENCHMARK", JSON.stringify({ ...result, samples: undefined }));
                assert.ok(samples.every(state => state.Phase === "Playing" && state.Enemies >= load && state.Projectiles > 0), "Fixture must remain populated and simulate, not freeze or die");
                assert.ok(summarize(samples.map(state => state.Projectiles)).mean >= (load === 320 ? 1200 : load === 180 ? 450 : 150), "Average post-collision projectile population must retain pressure, allowing same-tick collisions and expiry");
                if (enforce) {
                    assert.ok(result.metrics.Fps.mean >= 55, "Mean rendered FPS must be at least 55");
                    assert.ok(Math.min(...samples.map(sample => sample.Fps)) >= 45, "Sampled rendered FPS must remain at least 45");
                    assert.ok(result.simulatedTicksPerSecond >= 55, "Simulation must retain real-time speed");
                    assert.ok(result.metrics.SimulationMilliseconds.p95 <= 16.7, "p95 simulation step must fit a 60 Hz frame");
                    assert.ok(result.metrics.BatchMilliseconds.mean <= 5, "Mean batch preparation budget is 5 ms");
                }
            } catch (error) {
                report.failures.push({ load, error: String(error.stack || error) });
                console.error("COMBAT_BENCHMARK_FAILURE", load, error);
            } finally {
                await bounded(context.close());
            }
        }
        report.passed = report.failures.length === 0;
        if (!report.passed) process.exitCode = 1;
    } catch (error) {
        report.passed = false;
        report.failures.push({ stage: "browser-lifecycle", error: String(error.stack || error) });
        process.exitCode = 1;
    } finally {
        try { await bounded(browser.close()); }
        catch (error) {
            report.passed = false;
            report.failures.push({ stage: "browser-close", error: String(error.stack || error) });
            process.exitCode = 1;
        } finally {
            host.server.closeAllConnections();
            await new Promise(resolve => host.server.close(resolve));
            await fs.mkdir(path.dirname(output), { recursive: true });
            await fs.writeFile(output, JSON.stringify(report, null, 2), "utf8");
        }
    }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
