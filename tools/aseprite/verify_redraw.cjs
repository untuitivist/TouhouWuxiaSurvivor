const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const crypto = require("node:crypto");
const { execFileSync } = require("node:child_process");
const { PNG } = require("pngjs");
const root = path.resolve(__dirname, "../..");
const executable = process.env.ASEPRITE_EXE || "D:/thesteam/steamapps/common/Aseprite/Aseprite.exe";
const output = path.join(root, "artifacts/aseprite-redraw-verification");
fs.mkdirSync(output, { recursive: true });
const manifest = JSON.parse(fs.readFileSync(path.join(root, "art/redraw/manifest.json"), "utf8"));
const names = new Set();
const report = { editor: execFileSync(executable, ["--version"], { encoding: "utf8" }).trim(), assets: [], passed: false };
for (const entry of manifest.assets) {
    assert.match(entry.name, /^[a-z_]+\/[a-z_-]+$/);
    assert.ok(!names.has(entry.name));
    names.add(entry.name);
    const source = path.join(root, "art/redraw", entry.name + ".aseprite");
    const texture = path.join(root, "assets/aseprite/redraw", entry.name + ".png");
    const sourceBytes = fs.readFileSync(source);
    assert.equal(sourceBytes.readUInt16LE(4), 0xa5e0);
    const roundtrip = path.join(output, entry.name.replaceAll("/", "-") + ".png");
    execFileSync(executable, ["--batch", source, "--save-as", roundtrip], { encoding: "utf8" });
    const actual = PNG.sync.read(fs.readFileSync(texture));
    const expected = PNG.sync.read(fs.readFileSync(roundtrip));
    assert.equal(actual.width, entry.width);
    assert.equal(actual.height, entry.height);
    assert.equal(actual.width, expected.width);
    assert.equal(actual.height, expected.height);
    assert.ok(actual.data.equals(expected.data), entry.name + " source/export mismatch");
    assert.ok(actual.data.some((value, index) => index % 4 === 3 && value > 0), entry.name + " is blank");
    if (/^(players|actors)\//.test(entry.name)) {
        assert.equal(actual.width, 192);
        assert.equal(actual.height, 48);
        for (let frame = 0; frame < 4; frame++) {
            let covered = 0;
            for (let row = 0; row < 48; row++) for (let column = 0; column < 48; column++)
                if (actual.data[(row * 192 + frame * 48 + column) * 4 + 3] > 0) covered++;
            assert.ok(covered > 100, entry.name + " has an empty animation frame");
        }
    }
    const reference = path.join(root, "assets/internal_original/base", entry.name + ".png");
    if (fs.existsSync(reference)) {
        const old = PNG.sync.read(fs.readFileSync(reference));
        assert.ok(actual.width !== old.width || actual.height !== old.height || !actual.data.equals(old.data), entry.name + " copies original pixels");
    }
    report.assets.push({ ...entry, sourceSha256: crypto.createHash("sha256").update(sourceBytes).digest("hex"), textureSha256: crypto.createHash("sha256").update(fs.readFileSync(texture)).digest("hex"), roundtrip: true });
}
assert.equal(names.size, 54);
const approvedTitle = PNG.sync.read(fs.readFileSync(path.join(root, "assets/ui/title/moonlit_shrine.png")));
const currentTitle = PNG.sync.read(fs.readFileSync(path.join(root, "assets/aseprite/redraw/scenery/title_shrine.png")));
assert.equal(currentTitle.width, approvedTitle.width);
assert.equal(currentTitle.height, approvedTitle.height);
assert.ok(currentTitle.data.equals(approvedTitle.data), "Startup background must match the approved Aseprite reference");
for (const module of ["draw_redraw", "redraw_brush", "redraw_actors", "redraw_effects", "redraw_scenery", "redraw_ui", "redraw_title", "title_painter"]) {
    const script = fs.readFileSync(path.join(__dirname, module + ".lua"), "utf8");
    assert.ok(!/app\.open|getPixel|drawImage|internal_original/.test(script), "Redraw tools must not import reference pixels: " + module);
}
for (const file of fs.readdirSync(path.join(root, "game/presentation")).filter(name => name.endsWith(".cs"))) {
    const code = fs.readFileSync(path.join(root, "game/presentation", file), "utf8");
    if (file !== "GameAudio.cs") assert.ok(!code.includes("assets/internal_original"), "Runtime visual reference leak: " + file);
}
for (const script of ["tools/platform/build_web.ps1", "tools/rebirth/build_windows.ps1"]) {
    const code = fs.readFileSync(path.join(root, script), "utf8");
    assert.ok(code.includes("'assets/aseprite/redraw'"));
    assert.ok(code.includes("'assets/internal_original/base/audio'"));
    assert.ok(!code.includes("'assets/internal_original/base'"));
}
for (const file of ["project.godot", "export_presets.cfg"]) assert.ok(fs.readFileSync(path.join(root, file), "utf8").includes("assets/aseprite/redraw/ui/night_journal.png"));
report.passed = true;
fs.writeFileSync(path.join(output, "report.json"), JSON.stringify(report, null, 2) + "\n", "utf8");
console.log("ASEPRITE_REDRAW_VERIFICATION_PASS", names.size);
