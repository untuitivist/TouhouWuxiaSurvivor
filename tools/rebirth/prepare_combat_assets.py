import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
SOURCES = {
    'bullets': 'TH10 东方风神录/ANM/ANM/bullet/etama.png',
    'items': 'TH10 东方风神录/ANM/ANM/bullet/etama2.png',
    'enemy': 'TH10 东方风神录/ANM/ANM/enemy/enemy.png',
    'grass': 'TH10 东方风神录/ANM/ANM/background/stg1bg.png',
}
CROPS = {
    'red_pellet': ('bullets', (16, 32, 32, 48)),
    'violet_pellet': ('bullets', (64, 32, 80, 48)),
    'ofuda': ('bullets', (16, 112, 32, 128)),
    'star': ('bullets', (208, 160, 224, 176)),
    'stardust': ('bullets', (64, 160, 80, 176)),
    'dream': ('bullets', (32, 208, 64, 240)),
    'experience': ('items', (16, 208, 32, 224)),
    'healing': ('items', (64, 208, 80, 224)),
    'orb': ('enemy', (0, 192, 32, 224)),
    'grass': ('grass', (0, 0, 256, 256)),
}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path, default=ROOT.parent / '东方原作弹幕游戏素材包/东方Project游戏解包内容')
    arguments = parser.parse_args()
    destination = ROOT / 'assets/internal_original/base/combat'
    destination.mkdir(parents=True, exist_ok=True)
    source_images = {name: Image.open(arguments.source / source).convert('RGBA') for name, source in SOURCES.items()}
    records = []
    crops = {}
    for name, (source, bounds) in CROPS.items():
        image = source_images[source].crop(bounds)
        output = destination / (name + '.png')
        image.save(output)
        crops[name] = image
        records.append({'file': output.relative_to(ROOT).as_posix(), 'source': SOURCES[source], 'crop': list(bounds), 'sha256': hashlib.sha256(output.read_bytes()).hexdigest()})
    icon = Image.new('RGBA', (256, 256), '#17272d')
    draw = ImageDraw.Draw(icon)
    draw.rectangle((8, 8, 247, 247), fill='#c4a169')
    draw.rectangle((14, 14, 241, 241), fill='#604431')
    draw.rectangle((20, 20, 235, 235), fill='#192b30')
    for horizontal, vertical in ((20, 20), (220, 20), (20, 220), (220, 220)):
        draw.rectangle((horizontal, vertical, horizontal + 15, vertical + 15), fill='#b84343')
    icon.alpha_composite(crops['orb'].resize((192, 192), Image.Resampling.NEAREST), (32, 32))
    icon.alpha_composite(crops['ofuda'].resize((32, 48), Image.Resampling.NEAREST), (22, 172))
    icon.alpha_composite(crops['star'].resize((40, 40), Image.Resampling.NEAREST), (194, 38))
    icon_path = ROOT / 'assets/branding/night_journal.png'
    icon.save(icon_path)
    records.append({'file': icon_path.relative_to(ROOT).as_posix(), 'derivedFrom': ['orb', 'ofuda', 'star'], 'composition': '192px original yin-yang orb, ofuda and star with an original pixel frame; no official-logo claim', 'sha256': hashlib.sha256(icon_path.read_bytes()).hexdigest()})
    manifest = {'usage': 'User-supplied original-game pack; internal prototype only, no public redistribution clearance implied.',
                'sources': [{'file': source, 'sha256': hashlib.sha256((arguments.source / source).read_bytes()).hexdigest()} for source in SOURCES.values()], 'outputs': records}
    (destination / 'source_manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print('ORIGINAL_COMBAT_ASSETS_PASS', len(records))


if __name__ == '__main__':
    main()
