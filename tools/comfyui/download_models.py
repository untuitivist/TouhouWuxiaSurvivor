import argparse
import hashlib
import json
import logging
import os
from pathlib import Path
import shutil
import time
import urllib.error
import urllib.request


ROOT = Path(__file__).resolve().parents[2]
MODEL_DIRECTORY = Path("D:/Comfy-Desktop/ComfyUI-Shared/models")


def read_manifest(filename):
    manifest = json.loads(filename.read_text(encoding="utf-8"))
    paths = set()
    for artifact in manifest["artifacts"]:
        relative = Path(artifact["path"])
        if relative.is_absolute() or ".." in relative.parts or len(relative.parts) != 2:
            raise ValueError("Model paths must name a file within one model directory")
        if artifact["path"] in paths or artifact["size"] <= 0:
            raise ValueError("Duplicate model path or invalid size")
        if len(artifact["sha256"]) != 64 or any(character not in "0123456789abcdef" for character in artifact["sha256"]):
            raise ValueError("Invalid model SHA-256")
        expected_url = f"https://huggingface.co/{artifact['repo']}/resolve/{artifact['revision']}/{artifact['source_path']}"
        if artifact["url"] != expected_url or len(artifact["revision"]) != 40:
            raise ValueError("Downloads must use pinned Hugging Face artifact URLs")
        paths.add(artifact["path"])
    if sum(artifact["size"] for artifact in manifest["artifacts"]) != manifest["total_bytes"]:
        raise ValueError("Manifest total does not match artifact sizes")
    return manifest


def verify_artifact(filename, artifact):
    logging.info("Verifying %s (%s bytes)", filename.name, artifact["size"])
    if filename.stat().st_size != artifact["size"]:
        raise RuntimeError(f"Wrong model size; existing file preserved: {filename}")
    with filename.open("rb") as stream:
        digest = hashlib.file_digest(stream, "sha256").hexdigest()
    if digest != artifact["sha256"]:
        raise RuntimeError(f"SHA-256 mismatch; existing file preserved: {filename}")
    logging.info("Verified SHA-256 %s", digest)


def verify_models(manifest, model_directory, required_paths=None):
    available = {artifact["path"]: artifact for artifact in manifest["artifacts"]}
    required = set(available) if required_paths is None else set(required_paths)
    if not required.issubset(available):
        raise ValueError(f"Workflow has unpinned model files: {sorted(required - available.keys())}")
    for relative in sorted(required):
        verify_artifact(model_directory / relative, available[relative])


def download_models(manifest, model_directory, proxy, minimum_speed_mbps=0):
    model_directory.mkdir(parents=True, exist_ok=True)
    remaining = 0
    for artifact in manifest["artifacts"]:
        destination = model_directory / artifact["path"]
        partial = destination.with_suffix(destination.suffix + ".part")
        if not destination.exists():
            remaining += max(0, artifact["size"] - (partial.stat().st_size if partial.exists() else 0))
    if shutil.disk_usage(model_directory).free < remaining + 1024 ** 3:
        raise RuntimeError(f"Insufficient disk space; need {remaining / 1e9:.2f} GB plus 1 GiB reserve")
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({"https": proxy} if proxy else {}))
    total = manifest["total_bytes"]
    completed = 0
    logging.info("Model %s; total %.2f GB; remaining network transfer %.2f GB", manifest["model"], total / 1e9, remaining / 1e9)
    for artifact in manifest["artifacts"]:
        destination = model_directory / artifact["path"]
        destination.parent.mkdir(parents=True, exist_ok=True)
        if destination.exists():
            verify_artifact(destination, artifact)
            completed += artifact["size"]
            continue
        partial = destination.with_suffix(destination.suffix + ".part")
        for attempt in range(1, 17):
            downloaded = partial.stat().st_size if partial.exists() else 0
            if downloaded > artifact["size"]:
                raise RuntimeError(f"Oversized partial preserved: {partial}")
            if downloaded == artifact["size"]:
                break
            request = urllib.request.Request(artifact["url"], headers={"Range": f"bytes={downloaded}-"})
            report_bytes = downloaded
            last_report = time.monotonic()
            transfer_started, start_bytes = last_report, downloaded
            logging.info("Downloading %s; attempt %s; resume at %.2f GB", destination.name, attempt, downloaded / 1e9)
            try:
                with opener.open(request, timeout=90) as response:
                    if response.status == 206:
                        if not response.headers.get("Content-Range", "").startswith(f"bytes {downloaded}-"):
                            raise RuntimeError("Incorrect Content-Range; partial preserved")
                    elif downloaded or response.status != 200:
                        raise RuntimeError("Server refused safe resume; partial preserved")
                    with partial.open("ab") as stream:
                        while chunk := response.read(4 * 1024 * 1024):
                            if downloaded + len(chunk) > artifact["size"]:
                                raise RuntimeError("Response exceeds pinned size; partial preserved")
                            stream.write(chunk)
                            downloaded += len(chunk)
                            now = time.monotonic()
                            if now - last_report >= 10 or downloaded == artifact["size"]:
                                speed = (downloaded - report_bytes) / max(now - last_report, 0.001)
                                aggregate = completed + downloaded
                                logging.info("Total %.1f%% | %.2f / %.2f GB | %.2f MB/s | ETA %.0fs | file %.1f%%", aggregate / total * 100, aggregate / 1e9, total / 1e9, speed / 1e6, (total - aggregate) / max(speed, 1), downloaded / artifact["size"] * 100)
                                report_bytes, last_report = downloaded, now
                                if now - transfer_started >= 60 and (downloaded - start_bytes) / (now - transfer_started) < minimum_speed_mbps * 1e6:
                                    raise TimeoutError("Slow connection; reconnect with resume")
                if downloaded != artifact["size"]:
                    raise OSError("Incomplete response")
                break
            except (OSError, TimeoutError, urllib.error.URLError) as error:
                logging.warning("Transfer interrupted (%s); partial retained", type(error).__name__)
                if attempt == 16:
                    raise
                time.sleep(15)
        verify_artifact(partial, artifact)
        if destination.exists():
            raise FileExistsError(f"Refusing to overwrite concurrent destination: {destination}")
        os.rename(partial, destination)
        completed += artifact["size"]
    logging.info("All %s model files verified; %.2f / %.2f GB complete. No other model or dependency installed.", len(manifest["artifacts"]), completed / 1e9, total / 1e9)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--models-directory", type=Path, default=MODEL_DIRECTORY)
    parser.add_argument("--proxy")
    parser.add_argument("--minimum-speed-mbps", type=float, default=0)
    parser.add_argument("--verify-only", action="store_true")
    arguments = parser.parse_args()
    log_directory = ROOT / "artifacts/comfyui-reference"
    log_directory.mkdir(parents=True, exist_ok=True)
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(message)s", handlers=[logging.StreamHandler(), logging.FileHandler(log_directory / "download-qwen-2511.log", encoding="utf-8")])
    manifest = read_manifest(arguments.manifest)
    if arguments.verify_only:
        verify_models(manifest, arguments.models_directory)
    else:
        download_models(manifest, arguments.models_directory, arguments.proxy, arguments.minimum_speed_mbps)
