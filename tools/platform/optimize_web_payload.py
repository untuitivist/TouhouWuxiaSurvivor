import argparse
import base64
from dataclasses import dataclass
import gzip
import hashlib
import json
from pathlib import Path
import re
import shutil
import struct


RUNTIME = ".godot/mono/publish/wasm32/"
MANIFEST = RUNTIME + ".dotnet-publish-manifest"
POLICY = "wasm-linked-static-libraries-v1"


@dataclass(frozen=True)
class Entry:
    path: str
    offset: int
    data: bytes


def digest(data):
    return hashlib.sha256(data).hexdigest()


def parse_pack(pack):
    if len(pack) < 112 or pack[:4] != b"GDPC":
        raise ValueError("Expected a standalone Godot PCK")
    version, major, minor, patch, flags, file_base, directory = struct.unpack_from("<IIIIIQQ", pack, 4)
    if version != 3 or major != 4 or flags != 2 or file_base != 112:
        raise ValueError("Only unencrypted, standalone Godot 4 PCK v3 is supported")
    if any(pack[40:file_base]) or directory < file_base or directory + 4 > len(pack):
        raise ValueError("Invalid PCK header or directory offset")
    count = struct.unpack_from("<I", pack, directory)[0]
    cursor = directory + 4
    entries = []
    paths = set()
    if count > (len(pack) - cursor) // 44:
        raise ValueError("Invalid PCK entry count")
    for entry_index in range(count):
        if cursor + 4 > len(pack):
            raise ValueError("Truncated PCK path length")
        length = struct.unpack_from("<I", pack, cursor)[0]
        cursor += 4
        if length == 0 or length % 4 or cursor + length + 36 > len(pack):
            raise ValueError("Invalid PCK path length")
        encoded = pack[cursor:cursor + length]
        path = encoded.rstrip(b"\0").decode("utf-8")
        parts = path.split("/")
        if not path or "\0" in path or "\\" in path or ":" in path or any(part in {"", ".", ".."} for part in parts) or path in paths:
            raise ValueError("Unsafe or duplicate PCK path")
        paths.add(path)
        cursor += length
        offset, size, checksum, entry_flags = struct.unpack_from("<QQ16sI", pack, cursor)
        cursor += 36
        start = file_base + offset
        if entry_flags != 0 or offset % 16 or start > directory or size > directory - start:
            raise ValueError("Unsupported or invalid PCK entry: " + path)
        data = pack[start:start + size]
        if hashlib.md5(data).digest() != checksum:
            raise ValueError("PCK checksum mismatch: " + path)
        entries.append(Entry(path, offset, data))
    if cursor != len(pack):
        raise ValueError("Unexpected trailing PCK data")
    boundary = 0
    for entry in sorted(entries, key=lambda item: item.offset):
        if entry.data and entry.offset < boundary:
            raise ValueError("Overlapping PCK entries")
        boundary = max(boundary, entry.offset + len(entry.data))
    return pack[:file_base], entries


def encode_pack(header, entries):
    output = bytearray(header)
    offsets = {}
    for entry in sorted(entries, key=lambda item: item.offset):
        output.extend(b"\0" * (-len(output) % 16))
        offsets[entry.path] = len(output) - len(header)
        output.extend(entry.data)
    output.extend(b"\0" * (-len(output) % 16))
    struct.pack_into("<Q", output, 32, len(output))
    output.extend(struct.pack("<I", len(entries)))
    for entry in entries:
        encoded = entry.path.encode("utf-8")
        encoded += b"\0" * (-len(encoded) % 4)
        output.extend(struct.pack("<I", len(encoded)))
        output.extend(encoded)
        output.extend(struct.pack("<QQ16sI", offsets[entry.path], len(entry.data), hashlib.md5(entry.data).digest(), 0))
    return bytes(output)


def optimize_pack(pack):
    header, entries = parse_pack(pack)
    removed = [entry for entry in entries if re.fullmatch(re.escape(RUNTIME) + r"[^/]+\.a", entry.path)]
    if any(not entry.data.startswith(b"!<arch>\n") for entry in removed):
        raise ValueError("A runtime .a entry is not a static archive")
    removed_paths = {entry.path for entry in removed}
    retained = [entry for entry in entries if entry.path not in removed_paths]
    manifest_entries = [entry for entry in retained if entry.path == MANIFEST]
    if removed and len(manifest_entries) != 1:
        raise ValueError("A single publish manifest is required for runtime archive exclusion")
    if removed:
        by_path = {entry.path: entry for entry in entries}
        lines = []
        listed = set()
        for line in manifest_entries[0].data.decode("utf-8").splitlines(keepends=True):
            fields = line.rstrip("\r\n").split("\t")
            if len(fields) != 2:
                raise ValueError("Unrecognized publish manifest")
            relative, checksum = fields
            path = RUNTIME + relative
            if path in listed or path not in by_path:
                raise ValueError("Invalid publish manifest entry: " + path)
            listed.add(path)
            expected = base64.b64encode(hashlib.sha512(by_path[path].data).digest()).decode("ascii")
            if checksum != expected:
                raise ValueError("Publish manifest checksum mismatch: " + path)
            if path not in removed_paths:
                lines.append(line)
        if not removed_paths.issubset(listed):
            raise ValueError("Excluded archives are missing from the publish manifest")
        manifest = "".join(lines).encode("utf-8")
        retained = [Entry(entry.path, entry.offset, manifest) if entry.path == MANIFEST else entry for entry in retained]
    optimized = encode_pack(header, retained) if removed else pack
    _, verified = parse_pack(optimized)
    before = {entry.path: entry.data for entry in entries if entry.path not in removed_paths and entry.path != MANIFEST}
    after = {entry.path: entry.data for entry in verified if entry.path != MANIFEST}
    if before != after:
        raise ValueError("Optimization changed retained runtime or game content")
    report = {
        "policy": POLICY,
        "before": {"bytes": len(pack), "sha256": digest(pack), "gzipBytes": len(gzip.compress(pack, compresslevel=9, mtime=0))},
        "after": {"bytes": len(optimized), "sha256": digest(optimized), "gzipBytes": len(gzip.compress(optimized, compresslevel=9, mtime=0))},
        "removed": [{"path": entry.path, "bytes": len(entry.data), "sha256": digest(entry.data)} for entry in removed],
        "retainedPayloadsUnchanged": True,
        "retainedPayloads": [{"path": path, "bytes": len(data), "sha256": digest(data)} for path, data in before.items()],
        "publishManifestUpdated": bool(removed),
    }
    return optimized, report


def optimize_site(source, output, report_path):
    source, output, report_path = source.resolve(), output.resolve(), report_path.resolve()
    if output.exists() or report_path.exists() or source == output or source in output.parents or output in source.parents or source in report_path.parents or output in report_path.parents:
        raise ValueError("Use a new, separate output directory and report; existing builds are immutable")
    files = list(source.iterdir())
    if any(not item.is_file() or item.is_symlink() or not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]*", item.name) for item in files):
        raise ValueError("Expected a flat, regular-file Web export")
    if any(item.suffix == ".gz" for item in files):
        raise ValueError("Optimize an original export before creating compressed representations")
    packed = (source / "index.pck").read_bytes()
    optimized, report = optimize_pack(packed)
    html = (source / "index.html").read_text(encoding="utf-8")
    matches = list(re.finditer(r"const GODOT_CONFIG = (\{[^\n]+\});", html))
    if len(matches) != 1 or "const TOUHOU_DOWNLOADS = null;" not in html:
        raise ValueError("Expected an unactivated Web entry")
    match = matches[0]
    configuration = json.loads(match.group(1))
    if configuration["fileSizes"]["index.pck"] != len(packed) or configuration["fileSizes"]["index.wasm"] != (source / "index.wasm").stat().st_size:
        raise ValueError("Export sizes do not match the input payloads")
    configuration["fileSizes"]["index.pck"] = len(optimized)
    html = html[:match.start(1)] + json.dumps(configuration, separators=(",", ":"), ensure_ascii=False) + html[match.end(1):]
    output.mkdir(parents=True, exist_ok=False)
    for item in files:
        if item.name not in {"index.pck", "index.html"}:
            shutil.copy2(item, output / item.name)
            if digest(item.read_bytes()) != digest((output / item.name).read_bytes()):
                raise ValueError("Copy changed a retained Web file: " + item.name)
    (output / "index.pck").write_bytes(optimized)
    (output / "index.html").write_text(html, encoding="utf-8", newline="\n")
    wasm = (source / "index.wasm").read_bytes()
    wasm_gzip_bytes = len(gzip.compress(wasm, compresslevel=9, mtime=0))
    report.update({"source": str(source), "output": str(output), "wasmUnchanged": True, "wasmSha256": digest(wasm),
                   "coreGzipBytesBefore": report["before"]["gzipBytes"] + wasm_gzip_bytes,
                   "coreGzipBytesAfter": report["after"]["gzipBytes"] + wasm_gzip_bytes})
    report_path.parent.mkdir(parents=True, exist_ok=True)
    with report_path.open("x", encoding="utf-8", newline="\n") as stream:
        json.dump(report, stream, ensure_ascii=False, indent=2)
        stream.write("\n")
    return report


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--report", required=True, type=Path)
    arguments = parser.parse_args()
    report = optimize_site(arguments.source, arguments.output, arguments.report)
    print("WEB_PAYLOAD_OPTIMIZED " + json.dumps({key: report[key] for key in ["coreGzipBytesBefore", "coreGzipBytesAfter", "wasmUnchanged"]}), flush=True)


if __name__ == "__main__":
    main()
