# Shared CSharp Threadless Web Validation

## Publication Follow-Up

The release gate reproduced the raw-response issue after the initial successful suites. A no-game comparison over the same PCK found 12 aborted events in 20 synthetic-stream reads, with all byte counts/hashes correct, versus zero in 20 native Blob reads. The final raw fallback consumes the original response natively while a cloned response reports progress from the same single HTTP request; it hands the completed Blob to the engine. Compressed downloads retain the streaming path. Publication now additionally requires 20 real-loader raw downloads with matching bytes/SHA-256 and no failed requests. The first unshipped Windows candidate is preserved rather than overwritten when rebuilding the release from the final fix commit.

The user subsequently authorized the alpha-0.1.0 dual-target release. The publication path now requires the same clean source commit for the standalone Windows EXE and threadless Web artifact, and four full browser suites (raw/gzip, isolated/unisolated). The stale Windows changelog smoke assertion is corrected to inspect settings/F3 in their historical alpha-0.0.9 entry; the current release is checked independently. Publication results are recorded in docs/deployment.md and local release receipts. The original validation-only scope and investigation below are preserved as history.

## Accepted Scope

- One maintained Godot game project, one C# gameplay implementation and one set of content/settings/input semantics for Windows and Web. Build staging is generated input, not a second game.
- The user authorizes adjustment or replacement of the Web export toolchain to investigate a genuinely shared-memory-free browser build. Do not replace the Windows toolchain or fork gameplay into JavaScript.
- Future native mobile export should reuse shared gameplay and existing touch controls; Android/iOS packaging is not part of this task and touch support alone does not constitute native-mobile readiness.
- Preserve concurrent alpha-0.0.10 release changes already present when this task started. Isolate toolchain experiments from the published/publishing build and its caches. No publication is authorized by this compatibility task.

## Plan

1. [complete] Pinned matching engine/runtime/SDK sources and installed isolated Linux build tools.
2. [complete] Compiled matching threadless C# template and SDK; no Windows/gameplay changes. Native compilation took about five minutes after dependencies were ready.
3. [complete] Exported the shared project; all five browser scenarios pass both without isolation/SAB and with isolation.
4. [complete] Recorded reproducible commands, evidence and remaining limits; changes are separated from the pre-existing release work.

## Validated Result

- The maintained `game/**/*.cs` files are unchanged. The same game project builds for Windows and the experimental C# Web exporter; the signature-catalog utility is build tooling, not another game.
- The first-frame failure was Mono `aot-runtime-wasm.c:187`: missing interpreter-to-native signature `IL`. The .NET generator scans P/Invoke/internal calls, not Godot's unmanaged function-pointer fields. Changing thread flags alone was insufficient; simply referencing GodotSharp also did not fill this gap.
- The tool now reflects the matching GodotSharp assembly, collects 28 unmanaged ABI signatures (including nested function pointers and aggregate returns), and generates a non-executed P/Invoke signature catalog for the standard .NET trampoline generator. Actual managed callbacks also come from the matching full GodotSharp assembly instead of manually maintained callback stubs.
- Source patches are recreated from the pinned source archive on each build, with exact-match guards and unchanged-file detection. Final templates omit the temporary native/JavaScript tracing; a symbol map is retained beside the template for later diagnostics.
- Raw-resource cancellation events were reproduced even after the game initialized. The loader now retains source Response objects during startup and directly drains the source reader to EOF. All ten subsequent full browser scenarios completed without failed requests; no ERR_ABORTED event is ignored.

## Evidence

- Browser build: `artifacts/web-builds/20260907-044657-040`. `build-manifest.json` records the source hashes, template hash, thread flag and generated preset hash.
- `verification-compatible-unisolated/report.json`: five scenarios pass with `crossOriginIsolated === false` and `typeof SharedArrayBuffer === "undefined"`.
- `verification-compatible-isolated/report.json`: the same five scenarios pass with isolation enabled. The same artifact is used, not two gameplay builds.
- Both suites cover desktop keyboard/settings/save-and-reload/audio, three-finger touch and cancel, rotation pause/F3, touch upgrades, and complete Reimu/Marisa journeys through victory. Real mobile hardware was not tested.
- Loading unit regressions: 8/8. No-isolation compressed browser regressions: normal/cache, stalled progress, disconnect, resource HTTP error, loader error and engine script error all pass.
- Windows 4.7.1/.NET 8 compilation passes with zero warnings/errors; core regressions pass 29/29; the real NVIDIA renderer passes five capture/benchmark scenarios. Desktop measurements do not establish phone FPS.
- The combined Windows UI smoke still fails at `game/presentation/Diagnostics.cs:217`: it assumes the current release text contains older settings/F3 release notes. The pre-existing working-tree release promotion makes that assertion stale. This unrelated test was not rewritten to conceal the failure. Earlier platform/settings/profile/render-cache checks in that run pass.

## Reproduce

Prerequisites: the repository's existing private Web 4.6.1 editor and Windows .NET 9 SDK bootstrap, stopped Ubuntu-22.04, network access for the pinned dependencies, and the existing Windows 4.7.1 installation. The wrappers use the configured local runtime paths, print logs through CMD, wait for completion, and terminate only the WSL distribution they started. First-time Linux prerequisites may be installed with `tools\threadless\build.cmd -InstallLinuxDependencies`.

```bat
tools\threadless\build.cmd
build_web.cmd -Threadless
tools\threadless\probe.cmd
tools\threadless\verify.cmd
tools\platform\verify_loading.cmd --compatible
tools\rebirth\verify.cmd
tools\rebirth\verify_render.cmd
```

- `artifacts/web-compatible-latest.json` points to the experimental compatible artifact; `artifacts/web-latest.json` and the default threaded build/publish path are not replaced.
- Server headers cannot turn a threaded WASM binary into a single-threaded one. The compatible binary itself no longer requires shared memory; removing COOP/COEP from the existing deployed threaded binary would not fix it.
- No Git push, version promotion, production deployment, new Windows release EXE, Android APK or iOS package is part of this validation. A future authorized release must explicitly select and deploy the validated compatible output alongside the matching Windows release.
- Xiaomi browser/device versions, Safari/WebKit and physical mobile FPS remain unverified. A browser must still support the remaining engine features. Native mobile packaging, lifecycle and store requirements require separate work; touch controls alone are not sufficient.

## Investigation History (Resolved Unless Noted)

- The initial frame failure is a direct native exit(1), not a caught JavaScript exception or an unguarded SharedArrayBuffer reference. Captured WASM call indices through an opt-in exit trace. Relinking the isolated template with function names retained will identify the native call chain; no game rules or Windows sources are changed to work around it.
- Initial native output proves that the single-thread renderer and C# GameRoot._Ready both run (SHARED_WEB_CHECKS_READY is printed); the failure occurs on subsequent frame processing. The template contains no unguarded SharedArrayBuffer reference. Added an opt-in diagnostic route to log Emscripten's underlying exception before its generic exit-status message; this instrumentation is not written into the production artifact.
- The first no-isolation browser probe confirms crossOriginIsolated=false and SharedArrayBuffer undefined, but the game does not initialize: the native runtime repeatedly exits with status 1. Export success is therefore not treated as compatibility success. Expanded the probe to preserve initial informational/native output as well as errors to identify the first failure, rather than hiding it behind a timeout.
- Shared-project threadless export succeeds. The existing working-tree release notes required six additional font glyphs, so the checked-in font was regenerated from its already verified cached source without altering release text. The HTML now receives the exporter's actual thread flag instead of always requiring threads; old shells still default to requiring threads.
- The first genuine template builds successfully with threads=no and both managed thread properties=false. Its output is godot.web.template_release.wasm32.nothreads.mono.zip, accompanied by matching Godot.NET.Sdk/GodotSharp packages. The existing maintained Web build script now has an optional -Threadless path with separate editor, NuGet and platform caches; the compatible artifact pointer/log are separate from the concurrently publishing build.
- Matching C# SDK/assemblies build successfully, and runtime-pack publishing reaches native linking. The next concrete blockers are missing Linux `pkg-config` and `libatomic.so.1`; the bootstrap now offers an explicit `-InstallLinuxDependencies` switch for Ubuntu's `pkg-config` and `libatomic1` packages. This changes only the selected WSL distribution, not Windows or the game's production toolchain.
- Isolated Linux .NET 9.0.317, wasm-tools, SCons 4.9.1 and Emscripten 4.0.11 installed successfully without changing the Windows SDK or installing global Linux packages. Matching Mono glue was generated by the existing 4.6.1 editor. The first build stopped safely at a source-patch guard because the pinned runtime-cache cleanup block contained an extra comment; matched the exact block before continuing. No cache deletion occurred.
- GitHub's anonymous API returned a rate-limit error; switched to pinned raw source URLs rather than retrying API calls. The installed editor's source revision `ComplexRobot/godot@b94985982` is available and its runtime project confirms threads=true. Prefer rebuilding this matching revision with coordinated threadless settings over mixing a new upstream ABI with the installed 4.6.1 editor.
- Upstream build workflow requires Linux for Web templates due to Windows command-line length limits, generates Mono glue using its matching editor, and builds the SDK/assemblies alongside templates. WSL has approximately 39 GiB RAM and sufficient disk space; dependency discovery is still pending.
- Existing production SDK and WASM template use shared memory. Changing only the JavaScript feature check or export preset is insufficient.
- Initial environment inspection finds Ubuntu-22.04 installed and stopped. No Docker or CMake is exposed on the Windows PATH. Linux dependencies must be inspected before deciding what to install.
- Primary upstream reference from the preceding audit: godotengine/godot PR 106125, raulsntos/godot revision aa1f5ffe8321bc57155f606197b5aa93dd9e662c, with threadless settings in both Browser.targets and GetRuntimePack. It is an experimental source route, not a verified binary for this game.
