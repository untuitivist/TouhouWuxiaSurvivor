# Per-work runtime mappings

Each official work owns one UTF-8 JSON file named after its stable content-pack ID. Every file contains an `entries` array using the same schema as `preview_mappings.json`.

The runtime merges files by filename and rejects duplicate `(sourceId, category, name)` keys. A normal mapping must not carry proxy metadata.

Every cross-work substitute must record non-empty `proxySourceWork`, `reasonZh`, and `reviewStatus` metadata. `proxySourceWork` uses the source work's stable content-pack ID, cannot equal `sourceId`, and `reviewStatus` must be `proxy-reviewed`. The matching build manifest must contain one `proxyAssets` entry with the same identity and exact `asset`, `kind`, and proxy metadata values.

An identity declared under a build manifest's `unavailableEntries` must exist in `CompendiumCatalog` and must not appear in any runtime mapping. This keeps an honest text fallback distinct from a reviewed visual substitute.
