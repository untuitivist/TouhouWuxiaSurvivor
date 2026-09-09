const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const crypto = require("node:crypto");
const { execFileSync } = require("node:child_process");
const { PNG } = require("pngjs");
const root = path.resolve(__dirname, "../..");
const executable = process.env.ASEPRITE_EXE || "D:/thesteam/steamapps/common/Aseprite/Aseprite.exe";
const visualCode = fs.readFileSync(path.join(root, "game/presentation/VisualAssets.cs"), "utf8");
const edition = visualCode.match(/const string Edition = "([a-z0-9-]+)"/)[1];
const sourceRoot = "art/" + edition;
const textureRoot = "assets/aseprite/" + edition;
const output = path.join(root, "artifacts/aseprite-complete-verification", edition);
fs.mkdirSync(output, { recursive: true });
const readJson = file => JSON.parse(fs.readFileSync(path.join(root, file), "utf8"));
const readPng = file => PNG.sync.read(fs.readFileSync(path.join(root, file)));
const hash = file => crypto.createHash("sha256").update(fs.readFileSync(path.join(root, file))).digest("hex");
const manifest = readJson(sourceRoot + "/manifest.json");
assert.equal(manifest.schema, 3);
assert.equal(manifest.editor, "Aseprite");
assert.equal(manifest.reference_pixels_in_export, false);
assert.equal(manifest.manual_mouse_painting, false);
const sourceHashes = new Map(manifest.assets.map(entry => [entry.name, hash(entry.source)]));
const logs = execFileSync(executable, ["--batch", "--script-param", "root=" + root, "--script-param", "edition=" + edition,
    "--script-param", "output=" + output, "--script", path.join(__dirname, "verify_complete_art.lua")], { encoding: "utf8" });
assert.ok(logs.includes("ASEPRITE_REOPEN_PASS"), logs);
const reopened = new Map(JSON.parse(fs.readFileSync(path.join(output, "aseprite-reopened.json"), "utf8")).map(entry => [entry.name, entry]));
const names = new Set();
const images = new Map();
const report = { editor: execFileSync(executable, ["--version"], { encoding: "utf8" }).trim(), edition, assets: [], visualApproval: manifest.visual_approval, passed: false };
function framePixels(image, left, top, width, height) {
    const rows = [];
    for (let row = top; row < top + height; row++) rows.push(image.data.subarray((row * image.width + left) * 4, (row * image.width + left + width) * 4));
    return Buffer.concat(rows);
}
for (const entry of manifest.assets) {
    assert.match(entry.name, /^[a-z_]+\/[a-z_-]+$/);
    assert.ok(!names.has(entry.name));
    names.add(entry.name);
    assert.equal(entry.source, sourceRoot + "/" + entry.name + ".aseprite");
    assert.equal(entry.texture, textureRoot + "/" + entry.name + ".png");
    const source = fs.readFileSync(path.join(root, entry.source));
    assert.equal(source.readUInt16LE(4), 0xa5e0);
    assert.equal(source.readUInt16LE(6), entry.frames);
    assert.equal(source.readUInt16LE(8), entry.source_width);
    assert.equal(source.readUInt16LE(10), entry.source_height);
    assert.equal(hash(entry.source), sourceHashes.get(entry.name), "Verification must not modify editable sources");
    const actual = readPng(entry.texture);
    const expected = PNG.sync.read(fs.readFileSync(path.join(output, entry.name.replaceAll("/", "-") + ".png")));
    assert.equal(actual.width, entry.width);
    assert.equal(actual.height, entry.height);
    assert.equal(actual.width, expected.width);
    assert.equal(actual.height, expected.height);
    assert.ok(actual.data.equals(expected.data), entry.name + " source/export mismatch");
    assert.ok(actual.data.some((value, index) => index % 4 === 3 && value > 0), entry.name + " is blank");
    assert.deepEqual(reopened.get(entry.name).layers, entry.layers, "Layer visibility or editability mismatch: " + entry.name);
    assert.ok(entry.layers.filter(layer => layer.visible && layer.editable).length >= 2, "Meaningful drawing layers required: " + entry.name);
    if (entry.frames > 1) {
        assert.equal(entry.frames, entry.columns * entry.rows);
        for (let row = 0; row < entry.rows; row++) {
            const distinct = new Set();
            for (let column = 0; column < entry.columns; column++) {
                const pixels = framePixels(actual, column * entry.frame_width, row * entry.frame_height, entry.frame_width, entry.frame_height);
                assert.ok(pixels.filter((value, index) => index % 4 === 3 && value > 0).length > 100, entry.name + " empty frame");
                if (column >= 1 && column <= 4) distinct.add(crypto.createHash("sha256").update(pixels).digest("hex"));
            }
            if (entry.name.startsWith("players/")) assert.equal(distinct.size, 4, entry.name + " duplicated walk phases: " + row);
        }
    }
    if (entry.name.startsWith("players/")) {
        assert.deepEqual([entry.source_width, entry.source_height, entry.frames, entry.columns, entry.rows], [64, 64, 32, 8, 4]);
        assert.deepEqual(entry.directions, ["front", "right", "back", "left"]);
        const tags = reopened.get(entry.name).tags;
        assert.equal(tags.length, 12);
        for (let row = 0; row < 4; row++) for (const [mode, first, last] of [["idle", 1, 1], ["walk", 2, 5], ["cast", 6, 8]]) {
            const tag = tags.find(tag => tag.name === entry.directions[row] + "_" + mode);
            assert.deepEqual([tag.first, tag.last], [row * 8 + first, row * 8 + last]);
        }
    }
    images.set(entry.name, actual);
    report.assets.push({ name: entry.name, frames: entry.frames, drawingLayers: entry.layers.filter(layer => layer.visible).length,
        sourceSha256: sourceHashes.get(entry.name), textureSha256: hash(entry.texture), roundtrip: true });
}
for (const legacy of readJson("art/redraw/manifest.json").assets) assert.ok(names.has(legacy.name), "Lost runtime coverage: " + legacy.name);
for (const name of ["portraits/reimu", "portraits/marisa", "boards/reimu", "boards/marisa", "scenery/courtyard_tiles", "atlas/actors"]) assert.ok(names.has(name));
const atlas = readJson(textureRoot + "/atlas/actors.json");
const atlasImage = images.get("atlas/actors");
for (let index = 0; index < atlas.sprites.length; index++) {
    const entry = atlas.sprites[index];
    const image = images.get(entry.name);
    assert.equal(entry.columns * entry.frame_width, image.width);
    assert.equal(entry.rows * entry.frame_height, image.height);
    assert.ok(entry.x >= 0 && entry.y >= 0 && entry.x + image.width <= atlasImage.width && entry.y + image.height <= atlasImage.height);
    assert.ok(framePixels(atlasImage, entry.x, entry.y, image.width, image.height).equals(image.data), "Atlas mismatch: " + entry.name);
    for (const previous of atlas.sprites.slice(0, index)) assert.ok(entry.x >= previous.x + previous.columns * previous.frame_width || previous.x >= entry.x + image.width ||
        entry.y >= previous.y + previous.rows * previous.frame_height || previous.y >= entry.y + image.height, "Atlas regions overlap");
}
for (const file of fs.readdirSync(path.join(root, "game/presentation")).filter(name => name.endsWith(".cs"))) {
    const code = fs.readFileSync(path.join(root, "game/presentation", file), "utf8");
    if (file !== "GameAudio.cs") assert.ok(!code.includes("assets/internal_original"), "Runtime visual reference leak: " + file);
    assert.ok(!code.includes("assets/aseprite/redraw"), "Stale runtime artwork: " + file);
}
for (const module of ["complete_characters", "complete_portraits", "complete_enemies", "complete_effects", "complete_scenery", "complete_ui", "title_painter"]) {
    const code = fs.readFileSync(path.join(__dirname, module + ".lua"), "utf8");
    assert.ok(!/getPixel|app\.open|fromFile|internal_original/.test(code), "Drawing must not extract reference pixels: " + module);
}
for (const script of ["tools/platform/build_web.ps1", "tools/rebirth/build_windows.ps1", "tools/aseprite/verify_redraw_native.ps1"]) {
    const code = fs.readFileSync(path.join(root, script), "utf8");
    assert.ok(code.includes("'" + textureRoot + "'"), "Stale build pack: " + script);
    assert.ok(!code.includes("'assets/internal_original/base'"));
}
for (const file of ["project.godot", "export_presets.cfg"]) assert.ok(fs.readFileSync(path.join(root, file), "utf8").includes(textureRoot + "/ui/night_journal.png"));
report.passed = true;
report.atlasRegions = atlas.sprites.length;
fs.writeFileSync(path.join(output, "report.json"), JSON.stringify(report, null, 2) + "\n", "utf8");
console.log("ASEPRITE_COMPLETE_VERIFICATION_PASS assets=" + names.size + " atlas_regions=" + atlas.sprites.length + " edition=" + edition);
