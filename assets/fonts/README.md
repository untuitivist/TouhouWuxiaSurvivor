# Shared Chinese font

`NightJournalSans.otf` is a renamed character subset of Noto Sans CJK SC Regular,
tag `Sans2.004`, distributed under the SIL Open Font License 1.1 in `OFL.txt`.
Glyph outlines are unchanged. No system-installed Chinese font is required.

Source: https://github.com/notofonts/noto-cjk/tree/Sans2.004

Upstream SHA-256: `2c76254f6fc379fddfce0a7e84fb5385bb135d3e399294f6eeb6680d0365b74b`.
The generation script validates this hash before modifying the output.

Run `tools/platform/prepare_font.cmd` after changing Chinese game strings or
`CHANGELOG.md`. Requires Python and fontTools (tested with 4.63.0). The script
caches the original under ignored `artifacts/font-source/`, selects all characters
in `game/**/*.cs` and the full changelog plus Latin, and preserves font timestamps.
`font_manifest.json` records the generator version, source/output hashes and size.
`subset_font.py --check` is a build gate for missing glyphs.

The shipped font, license and manifest are committed; the full upstream font
and build caches are not. Ship the license alongside both export targets.
