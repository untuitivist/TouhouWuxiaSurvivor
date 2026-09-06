param([switch]$Threadless)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$encoding = [Text.UTF8Encoding]::new($false)
$toolRoot = Join-Path $root 'artifacts/web-probe-20260906'
& "$root/tools/web_probe/bootstrap.ps1" -EditorVersion '4.6.1'
& 'C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' "$PSScriptRoot/subset_font.py" --check
if ($LASTEXITCODE -ne 0) { throw 'Bundled font is incomplete; run tools/platform/prepare_font.cmd.' }
$build = Join-Path $root ('artifacts/web-builds/' + [DateTime]::Now.ToString('yyyyMMdd-HHmmss-fff'))
$stage = Join-Path $build 'stage'
$site = Join-Path $build 'site/TouhouSurvivor'
New-Item -ItemType Directory -Force -Path $stage, $site | Out-Null
$paths = @('game', 'assets/branding', 'assets/fonts', 'assets/internal_original/base', 'assets/world/tiles', 'platform/web', 'project.godot', 'export_presets.cfg', 'TouhouWuxiaSurvivor.csproj', 'TouhouWuxiaSurvivor.sln', 'CHANGELOG.md')
$manifest = [Collections.Generic.List[object]]::new()
foreach ($relative in $paths) {
    $source = Join-Path $root $relative
    $destination = Join-Path $stage $relative
    New-Item -ItemType Directory -Force -Path (Split-Path $destination -Parent) | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination -Recurse
    foreach ($file in Get-ChildItem -LiteralPath $source -File -Recurse | Where-Object { $_.Extension -notin @('.import', '.uid') -and $_.Name -notin @('TouhouWuxiaSurvivor.csproj', 'export_presets.cfg') }) {
        $manifest.Add(@{ path = [IO.Path]::GetRelativePath($root, $file.FullName); sha256 = (Get-FileHash -LiteralPath $file.FullName).Hash })
    }
}
$projectPath = Join-Path $stage 'TouhouWuxiaSurvivor.csproj'
$projectText = [IO.File]::ReadAllText($projectPath)
if (-not $projectText.Contains('Sdk="Godot.NET.Sdk/4.7.1"')) { throw 'Unexpected maintained project SDK.' }
$projectText = $projectText.Replace('Sdk="Godot.NET.Sdk/4.7.1"', 'Sdk="Godot.NET.Sdk/4.6.1"')
$projectText = [regex]::Replace($projectText, '<TargetFramework(?: Condition="[^"\r\n]+")?>net[89]\.0</TargetFramework>', '')
$projectText = $projectText.Replace('<PropertyGroup>', '<PropertyGroup><GodotWebBuild>true</GodotWebBuild><TargetFramework>net9.0</TargetFramework>')
[IO.File]::WriteAllText($projectPath, $projectText, $encoding)
$projectHash = (Get-FileHash -LiteralPath $projectPath).Hash
$editor = Join-Path $toolRoot 'editor-4.6.1/Godot_v4.6.1-stable_mono_web_export_win64'
$feedDirectory = Join-Path $editor 'nuget'
$cacheRoot = $toolRoot
$cachePrefix = 'shared'
if ($Threadless) {
    $compatible = & "$root/tools/threadless/configure_export.ps1" -Root $root
    $editor = $compatible.Editor
    $feedDirectory = $compatible.Feed
    $cacheRoot = $compatible.Cache
    $cachePrefix = 'compatible'
    $presetPath = Join-Path $stage 'export_presets.cfg'
    $presetText = [IO.File]::ReadAllText($presetPath, $encoding)
    if (!$presetText.Contains('variant/thread_support=true')) { throw 'Unexpected maintained Web thread preset.' }
    [IO.File]::WriteAllText($presetPath, $presetText.Replace('variant/thread_support=true', 'variant/thread_support=false'), $encoding)
}
$presetHash = (Get-FileHash -LiteralPath "$stage/export_presets.cfg").Hash
$dotnet = Join-Path $toolRoot 'dotnet/dotnet.exe'
$godot = Join-Path $editor 'Godot_v4.6.1-stable_mono_web_export_win64_console.exe'
$env:GodotWebBuild = 'true'
$env:DOTNET_ROOT = Join-Path $toolRoot 'dotnet'
$env:DOTNET_ROOT_X64 = $env:DOTNET_ROOT
$env:DOTNET_CLI_HOME = Join-Path $cacheRoot "$cachePrefix-cli-home"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
$env:DOTNET_CLI_UI_LANGUAGE = 'en'
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
$env:NUGET_PACKAGES = Join-Path $cacheRoot "$cachePrefix-nuget-packages"
$env:NUGET_HTTP_CACHE_PATH = Join-Path $cacheRoot "$cachePrefix-nuget-http"
$env:APPDATA = Join-Path $cacheRoot "$cachePrefix-appdata"
$env:LOCALAPPDATA = Join-Path $cacheRoot "$cachePrefix-localappdata"
$env:PATH = $env:DOTNET_ROOT + ';' + $env:PATH
[IO.File]::WriteAllText("$build/global.json", '{"sdk":{"version":"9.0.317","rollForward":"disable"}}', $encoding)
$feed = [Security.SecurityElement]::Escape($feedDirectory)
$nuget = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources><clear /><add key="web" value="$feed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <packageSourceMapping>
    <packageSource key="web"><package pattern="Godot.*" /><package pattern="GodotSharp*" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
"@
[IO.File]::WriteAllText("$build/NuGet.Config", $nuget, $encoding)
[IO.File]::WriteAllText("$editor/_sc_", '', $encoding)
$templates = Join-Path $editor 'editor_data/export_templates/4.6.1.stable.mono'
New-Item -ItemType Directory -Force -Path $templates | Out-Null
if (!$Threadless) { Copy-Item -LiteralPath "$editor/web_debug.zip", "$editor/web_release.zip" -Destination $templates -Force }
Push-Location $stage
try {
    if (-not (Test-Path "$env:DOTNET_ROOT/packs/Microsoft.NET.Runtime.WebAssembly.Sdk/9.0.19")) {
        & $dotnet workload install wasm-tools --skip-manifest-update --configfile "$build/NuGet.Config" --verbosity minimal
        if ($LASTEXITCODE -ne 0) { throw 'Private Web workload installation failed.' }
    }
    & $dotnet build TouhouWuxiaSurvivor.csproj --configuration Debug --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'Shared C# Web compilation failed.' }
    & $godot --headless --path $stage --editor --import
    if ($LASTEXITCODE -ne 0) { throw 'Web resource import failed.' }
    & $godot --headless --path $stage --export-release Web "$site/index.html" 2>&1 | Tee-Object -FilePath "$build/export.log" -Encoding utf8NoBOM
    if ($LASTEXITCODE -ne 0 -or (Select-String -Path "$build/export.log" -Pattern '^ERROR:|error [A-Z]+[0-9]+:' -Quiet)) { throw 'Web exporter reported errors.' }
    foreach ($file in $manifest) {
        $copied = Join-Path $stage $file.path
        if ((Get-FileHash -LiteralPath $copied).Hash -ne $file.sha256) { throw "Build modified shared source: $($file.path)" }
    }
    if (-not (Test-Path "$site/index.html") -or -not (Select-String -Path "$build/export.log" -SimpleMatch 'TouhouWuxiaSurvivor.dll' -Quiet)) { throw 'Web output is incomplete.' }
    Copy-Item -LiteralPath "$root/assets/fonts/OFL.txt" -Destination "$site/FONT_LICENSE.txt"
    Copy-Item -LiteralPath "$root/platform/web/loader.js" -Destination "$site/index.loader.js"
    if ((Get-FileHash -LiteralPath $projectPath).Hash -ne $projectHash) { throw 'Editor modified staged project configuration.' }
    if ((Get-FileHash -LiteralPath "$stage/export_presets.cfg").Hash -ne $presetHash) { throw 'Editor modified staged export configuration.' }
    $summary = @{ sourceCommit = (& git -C $root rev-parse HEAD).Trim(); sourceDirty = [bool](& git -C $root status --porcelain); sourceFiles = $manifest.ToArray(); sourceUnmodifiedInStage = $true; projectOverrides = @('SDK 4.7.1 -> 4.6.1', 'Resolve GodotWebBuild=true and TargetFramework=net9.0 for exporter parser'); generatedMetadataExcluded = @('*.import', '*.uid'); webToolchain = 'Godot C# experimental 4.6.1 / .NET 9.0.317'; files = @(Get-ChildItem $site -File | ForEach-Object { @{ name=$_.Name; bytes=$_.Length; sha256=(Get-FileHash $_.FullName).Hash } }) }
    $summary.threadSupport = !$Threadless
    $summary.exportPresetSha256 = $presetHash
    if ($Threadless) {
        $summary.projectOverrides += 'Web variant/thread_support=false'
        $summary.webToolchain = 'Godot C# 4.6.1 threadless rebuild b94985982075d0c7d73bbced427516ce5f3e140f / .NET 9.0.317'
        $summary.templateSha256 = (Get-FileHash -LiteralPath $compatible.Template).Hash
    }
    [IO.File]::WriteAllText("$build/build-manifest.json", ($summary | ConvertTo-Json -Depth 6), $encoding)
    $pointer = if ($Threadless) { 'web-compatible-latest.json' } else { 'web-latest.json' }
    [IO.File]::WriteAllText("$root/artifacts/$pointer", (@{ build=$build; site=(Split-Path $site -Parent); manifest="$build/build-manifest.json" } | ConvertTo-Json), $encoding)
    Write-Host "SHARED_WEB_BUILD_PASS $site"
} finally { Pop-Location }
