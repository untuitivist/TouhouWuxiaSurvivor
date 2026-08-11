# Per-work build manifests

Each official work owns one UTF-8 JSON file named after its stable content-pack ID, for example `th07_pcb.json`.

Generated-content arrays are `scenes`, `gridStrips`, `staticStrips`, `portraits`, `compositePortraits`, `copies`, and `audioCopies`. Paths remain relative to the user-supplied original-asset root. Generated files must stay under the matching ASCII pack folder in `assets/internal_original/`.

`proxyAssets` declares every runtime visual borrowed from another work. Each entry must repeat the runtime identity and the exact `asset`, `kind`, `proxySourceWork`, `reasonZh`, and `reviewStatus` values. `proxySourceWork` uses the source work's stable content-pack ID, cannot equal `sourceId`, and `reviewStatus` must be `proxy-reviewed`.

`unavailableEntries` declares compendium identities that intentionally retain text fallback because no reviewed visual source exists. Each entry requires `sourceId`, `category`, `name`, and a non-empty Chinese `reasonZh`; the same identity cannot also appear in a runtime mapping.
