# One C# project, Windows and Web

## Maintenance boundary

The maintained project is the repository-root `project.godot` and
`TouhouWuxiaSurvivor.csproj`. Both targets compile `game/**/*.cs` and use the same
scenes, assets, character rules and profile schema. There is no separately
maintained Web game. `game/platform/WebEntry.cs` only supplies the executable
entry point required by the experimental Web toolchain.

Windows remains on Godot .NET 4.7.1 / .NET 8. Web is an experimental target pinned
to the community Godot .NET Web export 4.6.1 / private .NET SDK 9.0.317. This is
not a claim that stock Godot 4 can export C# to Web. See the earlier comparative
evidence in `csharp_web_probe.md`; 4.7.1 Web showed rendering problems in the
tested Edge whereas 4.6.1 rendered the same gameplay correctly.

## Build and verify

From the repository root on the configured Windows workstation:

```bat
tools\platform\prepare_font.cmd
tools\rebirth\verify.cmd
tools\rebirth\verify_settings.cmd
build_web.cmd
tools\platform\verify_web.cmd
```

Font regeneration is necessary only after adding Chinese game/changelog text.
The current wrappers use the workstation's bundled PowerShell 7, Python and Node
paths. Another workstation must supply equivalent tools and adjust those wrapper
paths; these absolute tool locations are not dependencies of the exported game.
Python requires fontTools, Node browser verification requires Playwright and Edge.
`WEB_TEST_BROWSER` can override the browser executable for a separate test run.

The Web script reuses verified downloads under `artifacts/web-probe-20260906`.
It never runs the upstream global installer, changes the system .NET SDK, or
installs/trusts a development certificate. First bootstrap needs network access
and several GB of disk space. Logs go to `artifacts/web-build.log`.

Every build creates `artifacts/web-builds/<timestamp>/stage` and `site/TouhouSurvivor`.
The generated project metadata selects SDK 4.6.1, resolves `GodotWebBuild=true`
and a single unconditional `TargetFramework=net9.0` because the editor's project
parser does not evaluate conditional frameworks. No gameplay source is rewritten.
The build manifest checks staged source/asset hashes and records these explicit
metadata overrides; `.import` and `.uid` are excluded as engine-generated metadata.
It records HEAD plus whether the source tree was dirty, so HEAD alone is not
misrepresented as the exact build input. Do not edit staging as a second project.

`artifacts/web-latest.json` identifies the last successful build. Its `site` field
is the parent of `TouhouSurvivor/`; `manifest` records exact artifact sizes/hashes.
The browser suite stores a JSON report and screenshots in `<build>/verification`.
Do not upload `stage`, tool caches, source, diagnostic profiles or logs.

Windows self-contained EXE export remains the existing release process. Daily
development does not bump `alpha-0.0.9`, replace a historical EXE, push, or deploy.

## Player-facing differences

- Keyboard controls and remapping remain shared. Touch adds independently owned
  movement/focus/dash pointers and direct pause/build buttons, not synthetic keys.
- Touch menus enlarge the main start, character, upgrade and settings targets;
  secondary menus still need real-phone readability and target-size review.
- Settings include audio, video, keyboard and touch. Desktop keeps display-mode
  preview/rollback; Web exposes only browser-relevant controls. F3 is also reachable
  from touch settings. Browser fullscreen remains subject to browser policy.
- Bundled Chinese glyphs replace the dependency on installed system fonts.
- Profile JSON is source-generated and uses Godot file APIs. Windows retains its
  existing application/user-data identity. Web persists in browser-origin storage;
  there is no cloud save or automatic cross-device/EXE synchronization. Clearing
  site data or private browsing can remove progress; backups remain the user's concern.
- Web audio starts after user input. Portrait orientation displays a horizontal-play
  prompt and pauses combat; returning to landscape does not auto-resume.

## Hosting at /TouhouSurvivor/

### Release contract

The user's release contract now has two deliverables: a self-contained Windows
EXE and an updated Web deployment from the same game version/source revision.
A release is only complete after both deliverables and their verification succeed;
otherwise report a partial release explicitly. Preserve old artifacts and a Web
rollback target. A Git push is still a separate action, not implied by deployment.

The user has authorized an initial Web deployment to
`https://allinagent.top/TouhouSurvivor/` on the existing Caddy server, plus GitHub
push and server-side clone/pull. The concrete workflow and rollback rules are in
`docs/deployment.md`. This first deployment trial does not itself authorize a
version bump or a new Windows release export.
Earlier no-deployment statements in this document describe the completed local
validation, not a prohibition on this newly authorized trial.

### Server configuration

Upload only the contents of `<build>/site/TouhouSurvivor/` to an equivalent static
directory on the existing HTTPS server. No C# server runtime is needed to serve
these files. Configure the subpath with a trailing-slash redirect, `.wasm` MIME
`application/wasm`, and isolation headers on HTML **and assets**. For example,
adapt this inside the existing Nginx HTTPS `server` block:

```nginx
location = /TouhouSurvivor {
    return 308 /TouhouSurvivor/;
}
location ^~ /TouhouSurvivor/ {
    root /srv/www;
    index index.html;
    include mime.types;
    types { application/wasm wasm; }
    add_header Cross-Origin-Opener-Policy same-origin always;
    add_header Cross-Origin-Embedder-Policy require-corp always;
    add_header Cache-Control "no-cache" always;
    try_files $uri $uri/ =404;
}
```

Here files reside in `/srv/www/TouhouSurvivor/`. Check the actual server's existing
MIME table includes `wasm`; if it does, omit the extra `types` line to avoid a
duplicate-extension warning. Preserve the existing domain's TLS and other routes.
Do not send missing `.wasm`/`.pck` requests to an SPA HTML fallback. Keep game
assets same-origin, or explicitly configure permitted cross-origin resources.
Use a complete versioned-directory switch during later deployments so an old
HTML page cannot fetch a mixture of old and new binary files.

The local test host is loopback-only and is not a production server. HTTPS is
required on the public domain; localhost's secure-context exception does not
apply to an arbitrary phone accessing a plain-HTTP LAN address.

## Acceptance limits

### Recorded local validation — 2026-09-06 / 2026-09-07

Build: `artifacts/web-builds/20260906-235133-689`; report:
`verification/report.json` under that build. Edge version: `152.0.4191.62`.

| Check | Result |
| --- | --- |
| Windows .NET 8 compile | 0 warnings, 0 errors |
| Core regression / balance | 26/26 pass |
| Windows UI, legacy profiles and settings | Smoke and complete display/settings captures pass |
| Web keyboard / F3 / pause / build / video | Pass |
| Web changed volume across page reload | Pass, persistent storage reported and no profile warning |
| Emulated mobile independent move / focus / dash / cancel | Pass |
| Portrait pause, landscape manual resume, touch settings / F3 | Pass |
| Two consecutive touch upgrade choices | Pass |
| Reimu / Marisa seed-42 whole simulation | Both observe a live boss and reach victory, 265.47 / 272.20 simulated seconds |
| Browser console events / failed requests | Zero across all five scenarios |
| Shared source fingerprints / font repeat generation | Match / byte-identical |

The original `alpha-0.0.9` Windows EXE remains unchanged, SHA-256
`E21BFEDEC06B3DD1F2E730A97776C5B14BF609F7361FC73D4854509C42043AE8`.
System .NET SDK remains 8.0.302. No release number, historical published changelog
section, remote branch, or server deployment was changed.

### Remaining acceptance

Consult the generated verification report for actual pass/fail evidence, not
canvas existence. The suite uses isolated diagnostic saves, verifies save/reload,
desktop keyboard input, mobile multi-touch emulation, upgrades, portrait pause,
and asynchronous deterministic runs through the boss and result. It does not
expose query-string cheat commands in the normal shell.

The locally validated site's raw files total approximately 98.2 MB before HTTP
compression. This is not a small instant-load mobile game; real-network startup,
HTTP compression and memory use remain deployment acceptance items.

Headless Edge with software rendering is not a real-device FPS benchmark. Android
Chrome and iOS Safari (memory pressure, audio, safe areas, interruptions, fullscreen
and persistent storage) still require physical-device checks before public use.
Existing game-art redistribution permission also remains unresolved; the bundled
font license does not authorize unrelated artwork or music. No server deployment
is performed by this change.
