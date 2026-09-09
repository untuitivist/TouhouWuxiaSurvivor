import hashlib
import logging
import os
from pathlib import Path
import time
import urllib.request


MODEL_NAME = "animagine-xl-4.0-opt.safetensors"
MODEL_REVISION = "2b7c1b397761bf5bd3cc42e5b39ec99314a75a96"
MODEL_SIZE = 6938350040
MODEL_SHA256 = "6327eca98bfb6538dd7a4edce22484a1bbc57a8cff6b11d075d40da1afb847ac"
MODEL_URL = f"https://huggingface.co/cagliostrolab/animagine-xl-4.0/resolve/{MODEL_REVISION}/{MODEL_NAME}"
MODEL_DIRECTORY = Path("D:/Comfy-Desktop/ComfyUI-Shared/models/checkpoints")
ROOT = Path(__file__).resolve().parents[2]


def check_model(filename):
    logging.info("Checking SHA-256: %s", filename)
    with filename.open("rb") as stream:
        digest = hashlib.file_digest(stream, "sha256").hexdigest()
    if filename.stat().st_size != MODEL_SIZE or digest != MODEL_SHA256:
        raise RuntimeError(f"Model validation failed; file preserved: {filename}")
    logging.info("Verified %s bytes; SHA-256 %s", MODEL_SIZE, digest)


def download_model():
    MODEL_DIRECTORY.mkdir(parents=True, exist_ok=True)
    destination = MODEL_DIRECTORY / MODEL_NAME
    if destination.exists():
        check_model(destination)
        return
    partial = destination.with_suffix(destination.suffix + ".part")
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({"https": "http://127.0.0.1:10090"}))
    for attempt in range(1, 13):
        downloaded = partial.stat().st_size if partial.exists() else 0
        if downloaded == MODEL_SIZE:
            break
        if downloaded > MODEL_SIZE:
            raise RuntimeError("Partial download is too large; preserved for inspection")
        request = urllib.request.Request(MODEL_URL, headers={"Range": f"bytes={downloaded}-"})
        start_bytes = downloaded
        started = last_report = time.monotonic()
        try:
            with opener.open(request, timeout=90) as response:
                if downloaded and (response.status != 206 or not response.headers.get("Content-Range", "").startswith(f"bytes {downloaded}-")):
                    raise RuntimeError("Server refused correct resume; partial download preserved")
                with partial.open("ab") as stream:
                    while chunk := response.read(4 * 1024 * 1024):
                        stream.write(chunk)
                        downloaded += len(chunk)
                        now = time.monotonic()
                        if now - last_report >= 10 or downloaded == MODEL_SIZE:
                            speed = (downloaded - start_bytes) / max(now - started, 0.001)
                            remaining = (MODEL_SIZE - downloaded) / max(speed, 1)
                            logging.info("%.1f%% | %.2f / %.2f GB | %.2f MB/s | ETA %.0fs", downloaded / MODEL_SIZE * 100, downloaded / 1e9, MODEL_SIZE / 1e9, speed / 1e6, remaining)
                            last_report = now
                            if now - started > 60 and speed < 2e6:
                                raise TimeoutError("Slow connection below 2 MB/s; reconnecting with resume")
            if downloaded != MODEL_SIZE:
                raise OSError(f"Incomplete response: {downloaded}/{MODEL_SIZE}")
            break
        except (OSError, TimeoutError) as error:
            logging.warning("Attempt %s failed: %s; partial retained", attempt, error)
            if attempt == 12:
                raise
            time.sleep(15)
    check_model(partial)
    os.rename(partial, destination)
    logging.info("Finished. No other model or dependency was installed.")


if __name__ == "__main__":
    log_directory = ROOT / "artifacts/comfyui-reference"
    log_directory.mkdir(parents=True, exist_ok=True)
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(message)s", handlers=[logging.StreamHandler(), logging.FileHandler(log_directory / "download.log", encoding="utf-8")])
    logging.info("User-authorized download: Animagine XL 4.0 OPT; total %.2f GB", MODEL_SIZE / 1e9)
    download_model()
