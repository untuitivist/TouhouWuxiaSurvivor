import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
SOURCES = {
    "tree_canopy_a": ("TH13.5 东方心绮楼/backGround/bg01/梩偭傁彫01.png", "Original foliage viewed as a top-down scenery canopy; not a newly painted tree"),
    "tree_canopy_b": ("TH13.5 东方心绮楼/backGround/bg01/梩偭傁彫02.png", "Second original foliage canopy for existing deterministic tree locations"),
    "torii": ("TH13.5 东方心绮楼/actor/mamizou/texture/torii.png", "Mamizou actor texture reused as non-colliding map scenery; not claimed as a Hakurei shrine stage asset"),
    "title_shrine": ("TH15.5 东方凭依华/event/pic/bg/bg_hakurei.png", "Original monochrome Hakurei shrine event illustration, not a night-scene recreation"),
}


def prepare(source, check):
    destination = ROOT / "assets/internal_original/base/scenery"
    records = []
    for name, (relative, usage) in SOURCES.items():
        original = source / relative
        data = original.read_bytes()
        output = destination / (name + ".png")
        with Image.open(original) as image:
            size = list(image.size)
            alpha_range = image.convert("RGBA").getchannel("A").getextrema()
        if alpha_range != (0, 255):
            raise ValueError("Expected visible pixels and transparent outer edges: " + name)
        if check:
            if output.read_bytes() != data:
                raise ValueError("Scenery asset differs from original file: " + name)
        else:
            destination.mkdir(parents=True, exist_ok=True)
            output.write_bytes(data)
        records.append({"file": output.relative_to(ROOT).as_posix(), "source": relative,
                        "pixels": size, "crop": None, "transformation": "Byte-for-byte PNG copy; no repainting, alpha edits or resampling",
                        "source_sha256": hashlib.sha256(data).hexdigest(),
                        "output_sha256": hashlib.sha256(output.read_bytes()).hexdigest(), "usage": usage})
    manifest = {"usage": "User-supplied original pack; provenance does not grant redistribution permission.",
                "composition": "Game-authored placement, sizing and title modulation; original texture pixels remain unchanged.",
                "outputs": records}
    serialized = json.dumps(manifest, ensure_ascii=False, indent=2) + "\n"
    manifest_path = destination / "source_manifest.json"
    if check:
        if manifest_path.read_text(encoding="utf-8") != serialized:
            raise ValueError("Scenery provenance manifest changed")
    else:
        manifest_path.write_text(serialized, encoding="utf-8")
    print("ORIGINAL_SCENERY_PASS", "check" if check else "extract", len(records))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=ROOT.parent / "东方原作弹幕游戏素材包/东方Project游戏解包内容")
    parser.add_argument("--check", action="store_true")
    arguments = parser.parse_args()
    prepare(arguments.source, arguments.check)


if __name__ == "__main__":
    main()
