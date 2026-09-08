const assert = require("node:assert/strict");
const fs = require("node:fs/promises");
const path = require("node:path");
const { chromium } = require("playwright");
const { startDeploymentFixture } = require("./deployment_fixture.cjs");

async function main() {
    const root = path.resolve(__dirname, "../..");
    const build = JSON.parse(await fs.readFile(path.join(root, "artifacts/web-compatible-latest.json"), "utf8"));
    const output = path.join(build.build, "language-verification");
    await fs.mkdir(output, { recursive: true });
    let args = ["--", "--web-validation"];
    const host = await startDeploymentFixture(build, { isolation: false, entryArguments: () => args });
    const browser = await chromium.launch({ executablePath: "C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe", headless: true, args: ["--enable-unsafe-swiftshader"] });
    const report = { build: build.build, physicalDeviceTested: false, checks: [] };
    let activePage;
    try {
        for (const mobile of [false, true]) {
            const context = await browser.newContext({ viewport: mobile ? { width: 844, height: 390 } : { width: 1280, height: 720 }, hasTouch: mobile, isMobile: mobile });
            const page = await context.newPage();
            activePage = page;
            const errors = [];
            page.on("pageerror", error => errors.push(String(error)));
            page.on("console", message => { if (message.type() === "error") errors.push(message.text()); });
            const state = () => page.evaluate(() => window.__touhouProbe);
            const ready = () => page.waitForFunction(() => window.__touhouProbe && !document.querySelector("#loading"), {}, { timeout: 120000 });
            const click = async (text, name) => {
                const control = (await state()).Controls.find(value => name ? value.Name === name : value.Text === text);
                assert.ok(control, `Missing control ${name || text}`);
                const point = await page.evaluate(control => {
                    const scale = Math.min(innerWidth / 1280, innerHeight / 720);
                    return { x: (innerWidth - 1280 * scale) / 2 + (control.X + control.Width / 2) * scale, y: (innerHeight - 720 * scale) / 2 + (control.Y + control.Height / 2) * scale };
                }, control);
                if (mobile) await page.touchscreen.tap(point.x, point.y); else await page.mouse.click(point.x, point.y);
                await page.waitForTimeout(300);
            };
            await page.goto(host.origin + "/TouhouSurvivor/");
            await ready();
            assert.equal((await state()).Language, "zh");
            assert.equal(await page.evaluate(() => typeof SharedArrayBuffer), "undefined");
            await click("游戏设置");
            await click(null, "game_language");
            await page.keyboard.press("ArrowDown");
            await page.keyboard.press("ArrowDown");
            await page.keyboard.press("Enter");
            await page.waitForFunction(() => window.__touhouProbe.Language === "en");
            assert.ok((await state()).Controls.some(control => control.Name === "game_language" && control.Text === "English"));
            await page.screenshot({ path: path.join(output, mobile ? "touch-english.png" : "desktop-english.png") });
            await page.waitForTimeout(1000);
            await page.reload();
            await ready();
            assert.equal((await state()).Language, "en");
            await click("Game Settings");
            await click(null, "game_language");
            await page.keyboard.press("ArrowUp");
            await page.keyboard.press("ArrowUp");
            await page.keyboard.press("Enter");
            await page.waitForFunction(() => window.__touhouProbe.Language === "zh");
            await page.waitForTimeout(1000);
            await page.reload();
            await ready();
            assert.equal((await state()).Language, "zh");
            assert.deepEqual(errors, []);
            report.checks.push({ mobile, passed: true, persistedBothDirections: true, errors });
            await context.close();
        }
        args = ["--", "--rebirth-language-smoke"];
        const page = await browser.newPage();
        const events = [];
        page.on("console", message => events.push({ type: message.type(), text: message.text() }));
        page.on("pageerror", error => events.push({ type: "error", text: String(error) }));
        await page.goto(host.origin + "/TouhouSurvivor/");
        const deadline = Date.now() + 180000;
        while (!events.some(event => event.text.includes("LANGUAGE_SMOKE_PASS") || event.type === "error") && Date.now() < deadline) await page.waitForTimeout(500);
        assert.ok(events.some(event => event.text.includes("LANGUAGE_SMOKE_PASS")), JSON.stringify(events));
        assert.deepEqual(events.filter(event => event.type === "error"), []);
        report.checks.push({ name: "shared-language-smoke", passed: true, events });
        report.passed = true;
        console.log("WEB_LANGUAGE_PASS", output);
    } finally {
        if (!report.passed && activePage && !activePage.isClosed()) {
            report.failureState = await activePage.evaluate(() => window.__touhouProbe).catch(() => null);
            await activePage.screenshot({ path: path.join(output, "failure.png") }).catch(() => {});
        }
        await browser.close();
        host.server.closeAllConnections();
        await new Promise(resolve => host.server.close(resolve));
        await fs.writeFile(path.join(output, "report.json"), JSON.stringify(report, null, 2), "utf8");
    }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
