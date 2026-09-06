import argparse
import hashlib
import json
from pathlib import Path

import fontTools
from fontTools import subset
from fontTools.ttLib import TTFont


def required_characters(root):
    paths = sorted((root / 'game').rglob('*.cs')) + [root / 'CHANGELOG.md']
    characters = set(range(32, 127)) | set(range(160, 256))
    for source in paths:
        characters.update(ord(character) for character in source.read_text(encoding='utf-8') if ord(character) >= 32)
    return characters


def main():
    arguments = argparse.ArgumentParser()
    arguments.add_argument('--check', action='store_true')
    options = arguments.parse_args()
    root = Path(__file__).resolve().parents[2]
    destination = root / 'assets/fonts/NightJournalSans.otf'
    characters = required_characters(root)
    if options.check:
        with TTFont(destination) as font:
            missing = sorted(characters - set(font.getBestCmap()))
        if missing:
            raise SystemExit('Missing bundled glyphs: ' + ', '.join(f'U+{code:04X}' for code in missing))
        print(f'FONT_COVERAGE_PASS: {len(characters)} required characters')
        return
    source = root / 'artifacts/font-source/NotoSansCJKsc-Regular.otf'
    if hashlib.sha256(source.read_bytes()).hexdigest() != '2c76254f6fc379fddfce0a7e84fb5385bb135d3e399294f6eeb6680d0365b74b':
        raise SystemExit('Upstream font checksum mismatch')
    font = TTFont(source, recalcTimestamp=False)
    missing = characters - set(font.getBestCmap())
    if missing:
        raise SystemExit('Upstream font lacks required codepoints: ' + str(sorted(missing)))
    configuration = subset.Options()
    configuration.recalc_timestamp = False
    configuration.layout_features = ['*']
    configuration.name_IDs = ['*']
    configuration.name_languages = ['*']
    processor = subset.Subsetter(options=configuration)
    processor.populate(unicodes=characters)
    processor.subset(font)
    names = {1: 'NightJournal Sans', 2: 'Regular', 4: 'NightJournal Sans Regular', 6: 'NightJournalSans-Regular', 16: 'NightJournal Sans', 17: 'Regular'}
    for record in font['name'].names:
        if record.nameID in names:
            record.string = names[record.nameID].encode(record.getEncoding())
    if 'CFF ' in font:
        font['CFF '].cff.fontNames = ['NightJournalSans-Regular']
        font['CFF '].cff.topDictIndex[0].FullName = 'NightJournal Sans Regular'
        font['CFF '].cff.topDictIndex[0].FamilyName = 'NightJournal Sans'
    font.save(destination)
    font.close()
    manifest = {'upstream': 'notofonts/noto-cjk', 'tag': 'Sans2.004', 'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(), 'output_sha256': hashlib.sha256(destination.read_bytes()).hexdigest(), 'characters': len(characters), 'bytes': destination.stat().st_size, 'license': 'SIL Open Font License 1.1', 'modified': 'Character subset and renamed font family; glyph outlines unchanged.'}
    manifest['fonttools_version'] = fontTools.__version__
    (destination.parent / 'font_manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(manifest, indent=2))


if __name__ == '__main__':
    main()
