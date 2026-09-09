# Local ComfyUI Reference Tools

## Paused model migration — 2026-09-09

The user stopped the Qwen-Image-Edit-2511 migration to review a newly approved gameplay style reference. Do not automatically resume the download or start inference.

- `models/qwen-image-edit-2511.json` pins four artifact revisions, byte sizes and SHA-256 hashes; total 31,021,848,039 bytes.
- `download_models.py` supports resumable, verified downloads with aggregate progress, transfer speed and ETA. It has not completed downloading this bundle.
- About 6.93 GB of the first artifact remains in the shared model directory as a `.part` file. It is not a usable installed model. Keep it until the user decides whether to resume or remove it.
- The existing Animagine model and `generate_reimu_reference.py` are unchanged. No Qwen image-edit workflow has been implemented or executed.
- The current visual target and orb/ofuda correction are recorded in `docs/unified_art_direction.md` and `.NOTE.md`.
