const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { execFileSync } = require('node:child_process');
const { PNG } = require('pngjs');

const root = path.resolve(__dirname, '../..');
const sourcePath = path.join(root, 'art/title/moonlit_shrine.aseprite');
const texturePath = path.join(root, 'assets/ui/title/moonlit_shrine.png');
const roundtripPath = path.join(root, 'artifacts/aseprite-title-roundtrip.png');
const executable = process.env.ASEPRITE_EXE || 'D:/thesteam/steamapps/common/Aseprite/Aseprite.exe';
const source = fs.readFileSync(sourcePath);
assert.equal(source.readUInt32LE(0), source.length);
assert.equal(source.readUInt16LE(4), 0xa5e0);
assert.equal(source.readUInt16LE(6), 1);
assert.equal(source.readUInt16LE(8), 640);
assert.equal(source.readUInt16LE(10), 360);
assert.equal(source.readUInt16LE(12), 32);
assert.equal(source.readUInt16LE(132), 0xf1fa);
const chunks = source.readUInt32LE(140) || source.readUInt16LE(134);
let cursor = 144;
const layers = [];
for (let index = 0; index < chunks; index++) {
    const size = source.readUInt32LE(cursor);
    assert.ok(size >= 6 && cursor + size <= source.length);
    if (source.readUInt16LE(cursor + 4) === 0x2004) {
        const length = source.readUInt16LE(cursor + 22);
        layers.push(source.subarray(cursor + 24, cursor + 24 + length).toString('utf8'));
    }
    cursor += size;
}
assert.equal(layers.length, 10, 'Editable atmosphere, architecture, foliage and lighting layers');
assert.ok(layers.some(name => name.includes('Moon')) && layers.some(name => name.includes('torii')));
fs.mkdirSync(path.dirname(roundtripPath), { recursive: true });
const version = execFileSync(executable, ['--version'], { encoding: 'utf8' }).trim();
execFileSync(executable, ['--batch', sourcePath, '--save-as', roundtripPath], { encoding: 'utf8' });
const texture = PNG.sync.read(fs.readFileSync(texturePath));
const roundtrip = PNG.sync.read(fs.readFileSync(roundtripPath));
assert.equal(texture.width, 640);
assert.equal(texture.height, 360);
assert.equal(roundtrip.width, texture.width);
assert.equal(roundtrip.height, texture.height);
assert.ok(texture.data.equals(roundtrip.data), 'Runtime PNG matches the editable Aseprite source pixel-for-pixel');
const palette = new Set();
for (let offset = 0; offset < texture.data.length; offset += 4) {
    assert.equal(texture.data[offset + 3], 255, 'Opaque full-bleed title background');
    palette.add(texture.data.readUInt32LE(offset));
}
assert.ok(palette.size >= 20 && palette.size <= 256, 'Limited color pixel-art palette');
assert.ok(fs.readFileSync(path.join(root, 'game/presentation/GameCanvas.cs'), 'utf8').includes('res://assets/ui/title/moonlit_shrine.png'));
assert.ok(fs.readFileSync(path.join(root, 'tools/platform/build_web.ps1'), 'utf8').includes("'assets/ui/title'"));
const presets = fs.readFileSync(path.join(root, 'export_presets.cfg'), 'utf8');
assert.ok(!presets.includes('assets/ui/*'), 'Title assets must not be excluded from Windows or Web packages');
assert.equal((presets.match(/assets\/ui\/wuxia\/\*/g) || []).length, 3, 'Legacy UI remains excluded in all maintained presets');
const report = { version, width: texture.width, height: texture.height, layers, colors: palette.size,
    sourceSha256: crypto.createHash('sha256').update(source).digest('hex'),
    textureSha256: crypto.createHash('sha256').update(fs.readFileSync(texturePath)).digest('hex'),
    roundtripPixelMatch: true, passed: true };
fs.writeFileSync(path.join(root, 'artifacts/aseprite-title-verification.json'), JSON.stringify(report, null, 2) + '\n', 'utf8');
console.log('ASEPRITE_TITLE_VERIFICATION_PASS', JSON.stringify(report));
