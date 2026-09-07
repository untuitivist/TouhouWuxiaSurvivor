import argparse
import hashlib
import io
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
SOURCES = {
    "reimu": "TH07 东方妖妖梦/ANM/ANM/player/player00.png",
    "marisa": "TH08 东方永夜抄/ANM/ANM/player/player01.png",
    "spark": "TH08 东方永夜抄/ANM/ANM/player/player01b.png",
    "aura": "TH08 东方永夜抄/ANM/ANM/player/player00b.png",
    "ritual": "TH16 东方天空璋/ANM/ANM/effect/eff_magicsquare.png",
}
CROPS = {
    "master_spark": ("spark", (0, 0, 256, 128), "Marisa beam body and tapered emission end"),
    "marisa_cast": ("marisa", (64, 144, 128, 208), "Marisa star-ring charge and emission flare"),
    "reimu_seal": ("reimu", (128, 128, 256, 256), "Reimu-authored sealing pattern for the stationary field"),
    "reimu_seal_ink": ("reimu", (128, 128, 256, 256), "Reimu sealing ink with neutral atlas backing removed; raw crop is retained separately"),
    "reimu_talisman": ("reimu", (208, 0, 240, 32), "Reimu talisman at the four field corners"),
    "reimu_aura": ("aura", (0, 0, 96, 96), "Reimu aura for her spell and dream-orb impact"),
    "ritual_array": ("ritual", (0, 0, 256, 256), "Generic effect texture reused for this game's original optional seals, not a character spell"),
}


def prepare(source, check):
    destination = ROOT / "assets/internal_original/base/effects"
    images = {}
    records = []
    previews = []
    for name, (key, bounds, usage) in CROPS.items():
        if key not in images:
            with Image.open(source / SOURCES[key]) as image:
                images[key] = image.convert("RGBA")
        image = images[key]
        if not (0 <= bounds[0] < bounds[2] <= image.width and 0 <= bounds[1] < bounds[3] <= image.height):
            raise ValueError("Crop exceeds original image: " + name)
        cropped = image.crop(bounds)
        transformation = "RGBA crop only; no painted or generated gameplay pixels"
        if name == "reimu_seal_ink":
            red, green, blue, alpha = cropped.split()
            ink = ImageChops.subtract(red, ImageChops.darker(green, blue))
            cropped.putalpha(ImageChops.multiply(alpha, ink))
            transformation = "RGB unchanged; alpha multiplied by red minus min(green, blue), removing neutral backing without drawing new glyphs"
        if cropped.getchannel("A").getextrema() != (0, 255):
            raise ValueError("Expected both transparent and visible pixels: " + name)
        encoded = io.BytesIO()
        cropped.save(encoded, format="PNG")
        data = encoded.getvalue()
        output = destination / (name + ".png")
        if check:
            data = output.read_bytes()
            with Image.open(io.BytesIO(data)) as saved:
                if saved.size != cropped.size or saved.convert("RGBA").tobytes() != cropped.tobytes():
                    raise ValueError("Output pixels differ from original crop: " + name)
        else:
            destination.mkdir(parents=True, exist_ok=True)
            output.write_bytes(data)
        records.append({"file": output.relative_to(ROOT).as_posix(), "source": SOURCES[key],
                        "crop": list(bounds), "pixels": list(cropped.size), "usage": usage,
                        "transformation": transformation,
                        "sha256": hashlib.sha256(data).hexdigest()})
        previews.append((name, cropped))
    manifest = {"usage": "User-supplied original pack; provenance is not redistribution permission.",
                "animation": "Game-authored timing, layering and tint of original textures; not a frame-exact original spell recreation.",
                "sources": [{"file": filename, "sha256": hashlib.sha256((source / filename).read_bytes()).hexdigest()} for filename in SOURCES.values()],
                "outputs": records}
    serialized = json.dumps(manifest, ensure_ascii=False, indent=2) + "\n"
    manifest_path = destination / "source_manifest.json"
    if check:
        if manifest_path.read_text(encoding="utf-8") != serialized:
            raise ValueError("Source or manifest differs from the recorded extraction")
    else:
        manifest_path.write_text(serialized, encoding="utf-8")
        preview = Image.new("RGB", (768, 288 * ((len(previews) + 2) // 3)), "#122029")
        drawing = ImageDraw.Draw(preview)
        for index, (name, image) in enumerate(previews):
            left, top = index % 3 * 256, index // 3 * 288
            scale = min(240 / image.width, 240 / image.height)
            display = image.resize((round(image.width * scale), round(image.height * scale)), Image.Resampling.NEAREST)
            preview.paste(display, (left + (256 - display.width) // 2, top + 16 + (240 - display.height) // 2), display)
            drawing.text((left + 12, top + 264), name, fill="white")
        preview_path = ROOT / "artifacts/hero-art/source-crops.png"
        preview_path.parent.mkdir(parents=True, exist_ok=True)
        preview.save(preview_path)
    print("ORIGINAL_HERO_EFFECTS_PASS", "check" if check else "extract", len(records))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=ROOT.parent / "东方原作弹幕游戏素材包/东方Project游戏解包内容")
    parser.add_argument("--check", action="store_true")
    arguments = parser.parse_args()
    prepare(arguments.source, arguments.check)


if __name__ == "__main__":
    main()
