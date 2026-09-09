import argparse
import asyncio
import hashlib
import json
import logging
import os
from pathlib import Path
import socket
import sys
import time

import aiohttp

from download_animagine import check_model, MODEL_DIRECTORY, MODEL_NAME


ROOT = Path(__file__).resolve().parents[2]
COMFY_ROOT = Path("D:/Comfy-Desktop/ComfyUI-Installs/ComfyUI/ComfyUI")
OUTPUT = ROOT / "art/characters/reimu/reference-03"
LOG_DIRECTORY = ROOT / "artifacts/comfyui-reference"
BASE_URL = "http://127.0.0.1:8189"


async def stream_console(process):
    while line := await process.stdout.readline():
        logging.info("%s", line.decode("utf-8", errors="replace").rstrip())


async def generate(workflow, run_name, output_directory, pytorch_attention):
    check_model(MODEL_DIRECTORY / MODEL_NAME)
    with socket.socket() as probe:
        probe.bind(("127.0.0.1", 8189))
    workspace = LOG_DIRECTORY / "workspace"
    for folder in (workspace, workspace / "user", output_directory):
        folder.mkdir(parents=True, exist_ok=True)
    environment = dict(os.environ)
    environment.update(PYTHONUTF8="1", PYTHONUNBUFFERED="1", OMP_NUM_THREADS="8", MKL_NUM_THREADS="8", HF_HUB_OFFLINE="1", HF_HUB_DISABLE_TELEMETRY="1", DO_NOT_TRACK="1")
    command = [sys.executable, "-u", str(COMFY_ROOT / "main.py"), "--cpu", "--force-fp32", "--disable-all-custom-nodes", "--disable-api-nodes", "--disable-auto-launch", "--cache-none", "--listen", "127.0.0.1", "--port", "8189", "--base-directory", str(workspace), "--models-directory", str(MODEL_DIRECTORY.parent), "--output-directory", str(output_directory), "--user-directory", str(workspace / "user"), "--database-url", "sqlite:///:memory:"]
    if pytorch_attention:
        command.append("--use-pytorch-cross-attention")
    started = time.monotonic()
    process = await asyncio.create_subprocess_exec(*command, cwd=COMFY_ROOT, env=environment, stdout=asyncio.subprocess.PIPE, stderr=asyncio.subprocess.STDOUT)
    console = asyncio.create_task(stream_console(process))
    logging.info("Owned ComfyUI PID %s; CPU-only, 8 threads. Existing GPU tasks untouched.", process.pid)
    try:
        async with aiohttp.ClientSession(timeout=aiohttp.ClientTimeout(total=60)) as session:
            for attempt in range(90):
                if process.returncode is not None:
                    raise RuntimeError(f"ComfyUI exited with {process.returncode}")
                try:
                    async with session.get(BASE_URL + "/system_stats") as response:
                        response.raise_for_status()
                        system_stats = await response.json()
                        break
                except aiohttp.ClientError:
                    await asyncio.sleep(2)
            else:
                raise TimeoutError("ComfyUI startup exceeded three minutes")
            client_id = "reimu-reference"
            async with session.ws_connect(BASE_URL + "/ws?clientId=" + client_id, heartbeat=30, max_msg_size=16 * 1024 * 1024) as websocket:
                async with session.post(BASE_URL + "/prompt", json={"prompt": workflow, "client_id": client_id}) as response:
                    result = await response.json()
                    if response.status != 200:
                        raise RuntimeError(f"Workflow rejected: {result}")
                prompt_id = result["prompt_id"]
                logging.info("Queued prompt %s; seed %s", prompt_id, workflow["3"]["inputs"]["seed"])
                async with asyncio.timeout(7200):
                    async for message in websocket:
                        if message.type != aiohttp.WSMsgType.TEXT:
                            continue
                        event = json.loads(message.data)
                        data = event.get("data", {})
                        if data.get("prompt_id") not in (None, prompt_id):
                            continue
                        if event["type"] == "progress":
                            logging.info("Sampling %s / %s", data["value"], data["max"])
                        elif event["type"] in ("execution_error", "execution_interrupted"):
                            raise RuntimeError(f"Inference failed: {data}")
                        elif event["type"] == "execution_success":
                            break
                        elif event["type"] == "executing" and data.get("node") is None and data.get("prompt_id") == prompt_id:
                            break
                    else:
                        raise RuntimeError("ComfyUI connection ended before completion")
                async with session.get(BASE_URL + "/history/" + prompt_id) as response:
                    response.raise_for_status()
                    history = (await response.json())[prompt_id]
                images = history.get("outputs", {}).get("9", {}).get("images", [])
                if not images:
                    raise RuntimeError("No saved output image found")
                saved = []
                for image in images:
                    filename = (output_directory / image["subfolder"] / image["filename"]).resolve()
                    if not filename.is_relative_to(output_directory) or not filename.is_file():
                        raise RuntimeError("Invalid output image path")
                    saved.append({"file": filename.relative_to(output_directory).as_posix(), "sha256": hashlib.sha256(filename.read_bytes()).hexdigest()})
                record = {"stage": "AI reference only; not an Aseprite redraw", "prompt_id": prompt_id, "elapsed_seconds": round(time.monotonic() - started, 1), "device": "CPU", "threads": 8, "workflow": workflow, "system_stats": system_stats, "images": saved}
                with (output_directory / f"{run_name}.json").open("x", encoding="utf-8") as stream:
                    json.dump(record, stream, ensure_ascii=False, indent=2)
                    stream.write("\n")
                logging.info("REFERENCE_READY %s", saved)
    finally:
        if process.returncode is None:
            process.terminate()
            try:
                await asyncio.wait_for(process.wait(), 15)
            except TimeoutError:
                process.kill()
                await process.wait()
        await console
        logging.info("Stopped only owned ComfyUI PID %s", process.pid)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--workflow", type=Path, default=OUTPUT / "workflow-api.json")
    parser.add_argument("--run-name", default="run-01")
    parser.add_argument("--output-directory", type=Path, default=OUTPUT)
    parser.add_argument("--pytorch-attention", action="store_true")
    arguments = parser.parse_args()
    if not arguments.run_name.isascii() or not arguments.run_name.replace("-", "").isalnum():
        parser.error("run-name must contain only ASCII letters, digits and hyphens")
    output_directory = arguments.output_directory.resolve()
    if not output_directory.is_relative_to((ROOT / "art/characters/reimu").resolve()):
        parser.error("Output directory must stay inside art/characters/reimu")
    if (output_directory / f"{arguments.run_name}.json").exists():
        parser.error("Run already exists; choose a new run-name. Existing references are never overwritten.")
    LOG_DIRECTORY.mkdir(parents=True, exist_ok=True)
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(message)s", handlers=[logging.StreamHandler(), logging.FileHandler(LOG_DIRECTORY / f"{arguments.run_name}.log", encoding="utf-8")])
    workflow = json.loads(arguments.workflow.read_text(encoding="utf-8"))
    asyncio.run(generate(workflow, arguments.run_name, output_directory, arguments.pytorch_attention))
