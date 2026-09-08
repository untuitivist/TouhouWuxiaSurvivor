const assert = require("node:assert/strict");
const fs = require("node:fs/promises");
const path = require("node:path");
const { chromium } = require("playwright");
const { startDeploymentFixture } = require("./deployment_fixture.cjs");

const option = (name, fallback) => process.argv.find(value => value.startsWith(`--${name}=`))?.slice(name.length + 3) ?? fallback;
const summarize = values => {
    const sorted = [...values].sort((left, right) => left - right);
    return { mean: values.reduce((total, value) => total + value, 0) / values.length, p95: sorted[Math.floor(sorted.length * 0.95)] };
};

async function main() {
    const buildPath = path.resolve(option("build", "artifacts/web-compatible-latest.json"));
    const build = JSON.parse(await fs.readFile(buildPath, "utf8"));
    const output = path.resolve(option("output", path.join(build.build, "combat-benchmark.json")));
    const throttle = Number(option("throttle", "1"));
    assert.ok(Number.isFinite(throttle) && throttle >= 1);
    const report = { buildPath, build, throttle, isolation: option("isolation", "false") === "true", physicalDeviceTested: false, scope: "Desktop Edge, real fixed-step combat with compatible growth branches and replenished entities; CPU emulation is not phone FPS.", scenarios: [] };
    const host = await startDeploymentFixture(build, { isolation: report.isolation });
    const browser = await chromium.launch({ executablePath: "C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe", headless: true, args: ["--enable-unsafe-swiftshader"] });
    try {
        for (const load of option("loads", "40,180,320").split(",").map(Number)) {
            const context = await browser.newContext({ viewport: { width: 844, height: 390 }, hasTouch: true, isMobile: true, deviceScaleFactor: 1 });
            const page = await context.newPage();
            const errors = [];
            page.on("pageerror", error => errors.push(String(error)));
            await page.route("**/TouhouSurvivor/", async route => {
                const response = await route.fetch();
                const html = await response.text();
                assert.ok(html.includes('"args":[]'));
                await route.fulfill({ response, body: html.replace('"args":[]', `"args":["--","--web-validation","--web-fixture=combat-performance","--web-load=${load}"]`) });
            });
            await page.goto(host.origin + "/TouhouSurvivor/", { waitUntil: "domcontentloaded" });
            await page.waitForFunction(() => window.__touhouProbe?.Tick > 60 && !document.getElementById("loading"), {}, { timeout: 120000 });
            const capabilities = await page.evaluate(() => ({ isolated: crossOriginIsolated, sharedMemory: typeof SharedArrayBuffer }));
            assert.equal(capabilities.isolated, report.isolation);
            if (!report.isolation) assert.equal(capabilities.sharedMemory, "undefined");
            const session = await context.newCDPSession(page);
            await session.send("Emulation.setCPUThrottlingRate", { rate: throttle });
            await page.waitForTimeout(3000);
            const samples = await page.evaluate(() => new Promise(resolve => {
                const states = [];
                const interval = setInterval(() => states.push({ ...window.__touhouProbe, wall: performance.now() }), 100);
                setTimeout(() => { clearInterval(interval); resolve(states); }, 15000);
            }));
            assert.ok(samples.length > 0);
            const first = samples[0];
            const last = samples.at(-1);
            console.log("SYSTEM_TIMINGS", JSON.stringify(["encounters", "enemies", "grid", "weapons", "projectiles", "progression"].map((name, index) => ({ name, ...summarize(samples.map(sample => sample.SystemMilliseconds[index])) }))));
            const result = { load, simulatedTicksPerSecond: (last.Tick - first.Tick) / ((last.wall - first.wall) / 1000), metrics: Object.fromEntries(["Fps", "SimulationMilliseconds", "EventMilliseconds", "BatchMilliseconds", "DrawCalls", "BatchInstances"].map(key => [key, summarize(samples.map(sample => sample[key]))])), errors, samples };
            assert.ok(last.Tick > first.Tick);
            assert.deepEqual(errors, []);
            report.scenarios.push(result);
            result.capabilities = capabilities;
            console.log("COMBAT_BENCHMARK", JSON.stringify({ ...result, samples: undefined }));
            assert.ok(samples.every(state => state.Phase === "Playing" && state.Enemies >= load && state.Projectiles > 0), "Fixture must remain populated and simulate, not freeze or die");
            assert.ok(summarize(samples.map(state => state.Projectiles)).mean >= (load === 320 ? 1200 : load === 180 ? 450 : 150), "Average post-collision projectile population must retain pressure, allowing same-tick collisions and expiry");
            await context.close();
        }
    } finally {
        await browser.close();
        host.server.closeAllConnections();
        await new Promise(resolve => host.server.close(resolve));
        await fs.mkdir(path.dirname(output), { recursive: true });
        await fs.writeFile(output, JSON.stringify(report, null, 2), "utf8");
    }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
