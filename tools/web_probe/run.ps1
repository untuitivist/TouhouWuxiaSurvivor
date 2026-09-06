param([ValidateSet('prepare', 'workload', 'export')][string]$Stage = 'prepare', [ValidateSet('4.7.1', '4.6.1')][string]$EditorVersion = '4.7.1', [string]$SourceRevision = 'c56e1ef8c88d63523732b41611913dc5b81e8a67')

$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$probe = Join-Path $repository 'artifacts/web-probe-20260906'
$toolRoot = $probe
if ($EditorVersion -ne '4.7.1') { $probe = Join-Path $probe "compat-$EditorVersion" }
$project = Join-Path $probe 'project'
$editorFolder = if ($EditorVersion -eq '4.7.1') { 'editor' } else { "editor-$EditorVersion" }
$editor = Join-Path $toolRoot "$editorFolder/Godot_v$EditorVersion-stable_mono_web_export_win64"
$dotnet = Join-Path $toolRoot 'dotnet/dotnet.exe'
$godot = Join-Path $editor "Godot_v$EditorVersion-stable_mono_web_export_win64_console.exe"
$encoding = [Text.UTF8Encoding]::new($false)
$env:DOTNET_ROOT = Join-Path $toolRoot 'dotnet'
$env:DOTNET_ROOT_X64 = $env:DOTNET_ROOT
$env:DOTNET_CLI_HOME = Join-Path $probe 'cli-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
$env:DOTNET_CLI_UI_LANGUAGE = 'en'
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
$env:NUGET_PACKAGES = Join-Path $probe 'nuget-packages'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $probe 'nuget-http'
$env:APPDATA = Join-Path $probe 'appdata'
$env:LOCALAPPDATA = Join-Path $probe 'localappdata'
$env:PATH = $env:DOTNET_ROOT + ';' + $env:PATH

if ($Stage -eq 'prepare') {
    if (Test-Path -LiteralPath $project) { throw 'Project already exists; preserve it and run workload/export stages instead.' }
    New-Item -ItemType Directory -Force -Path $project | Out-Null
    $revision = (& git -C $repository rev-parse --verify "$SourceRevision^{commit}").Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Source revision is not a commit.' }
    & git -C $repository archive --format=tar --output="$probe/source.tar" $revision project.godot TouhouWuxiaSurvivor.csproj TouhouWuxiaSurvivor.sln game assets CHANGELOG.md
    if ($LASTEXITCODE -ne 0) { throw 'Snapshot failed.' }
    & tar.exe -xf "$probe/source.tar" -C $project
    if ($LASTEXITCODE -ne 0) { throw 'Snapshot extraction failed.' }
    [IO.File]::WriteAllText("$probe/source-revision.txt", $revision, $encoding)
    [IO.File]::WriteAllText("$probe/global.json", '{"sdk":{"version":"9.0.317","rollForward":"disable"}}', $encoding)
    $feed = [Security.SecurityElement]::Escape((Join-Path $editor 'nuget'))
    $nuget = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources><clear /><add key="experiment" value="$feed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <packageSourceMapping>
    <packageSource key="experiment"><package pattern="Godot.*" /><package pattern="GodotSharp*" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
"@
    [IO.File]::WriteAllText("$probe/NuGet.Config", $nuget, $encoding)
    [IO.File]::WriteAllText("$editor/_sc_", '', $encoding)
    $templates = Join-Path $editor "editor_data/export_templates/$EditorVersion.stable.mono"
    New-Item -ItemType Directory -Force -Path $templates | Out-Null
    Copy-Item -LiteralPath "$editor/web_debug.zip", "$editor/web_release.zip" -Destination $templates
    $projectFile = Join-Path $project 'TouhouWuxiaSurvivor.csproj'
    $source = [IO.File]::ReadAllText($projectFile, $encoding).Replace('<TargetFramework>net8.0</TargetFramework>', '<TargetFramework>net9.0</TargetFramework><OutputType>Exe</OutputType>')
    $source = $source.Replace('Godot.NET.Sdk/4.7.1', "Godot.NET.Sdk/$EditorVersion")
    $source = $source.Replace('<Compile Include="game/**/*.cs" />', '<Compile Include="game/**/*.cs" /><Compile Include="Program.cs" />')
    [IO.File]::WriteAllText($projectFile, $source, $encoding)
    $godotProject = Join-Path $project 'project.godot'
    $settings = [IO.File]::ReadAllText($godotProject, $encoding).Replace('[rendering]', "[rendering]`ntextures/vram_compression/import_etc2_astc=true")
    [IO.File]::WriteAllText($godotProject, $settings, $encoding)
    [IO.File]::WriteAllText("$project/Program.cs", "{}`n", $encoding)
    $preset = @'
[preset.0]
name="Web Probe"
platform="Web"
runnable=true
export_filter="all_resources"
include_filter="CHANGELOG.md"
exclude_filter="assets/audio/*,assets/characters/*,assets/combat/*,assets/ui/*,assets/internal_original/th*/*,assets/internal_original/mappings/*,assets/internal_original/base/scenes/*,assets/internal_original/base/portraits/*"
export_path="../site/TouhouSurvivor/index.html"
script_export_mode=2

[preset.0.options]
variant/extensions_support=false
variant/thread_support=true
vram_texture_compression/for_desktop=true
vram_texture_compression/for_mobile=true
html/export_icon=true
html/canvas_resize_policy=2
html/focus_canvas_on_start=true
progressive_web_app/enabled=false
dotnet/include_scripts_content=false
dotnet/include_debug_symbols=false
'@
    [IO.File]::WriteAllText("$project/export_presets.cfg", $preset, $encoding)
    New-Item -ItemType Directory -Force -Path "$probe/site/TouhouSurvivor" | Out-Null
    Write-Host "Prepared isolated project at $revision"
}

Push-Location $project
try {
    & $dotnet --version
    if ($LASTEXITCODE -ne 0) { throw 'SDK check failed.' }
    if ($Stage -eq 'workload') {
        & $dotnet workload install wasm-tools --skip-manifest-update --configfile "$probe/NuGet.Config" --verbosity minimal
        if ($LASTEXITCODE -ne 0) { throw 'Private wasm workload install failed.' }
    }
    if ($Stage -eq 'export') {
        if (-not (Test-Path "$project/TouhouWuxiaSurvivor.sln")) {
            $revision = [IO.File]::ReadAllText("$probe/source-revision.txt", $encoding).Trim()
            $solution = & git -C $repository show "${revision}:TouhouWuxiaSurvivor.sln"
            if ($LASTEXITCODE -ne 0) { throw 'Could not recover snapshot solution.' }
            [IO.File]::WriteAllText("$project/TouhouWuxiaSurvivor.sln", ($solution -join "`n").TrimStart([char]0xfeff), $encoding)
        }
        & $dotnet build TouhouWuxiaSurvivor.csproj --configuration Debug --verbosity minimal
        if ($LASTEXITCODE -ne 0) { throw 'C# editor build failed.' }
        & $godot --headless --path $project --editor --import
        if ($LASTEXITCODE -ne 0) { throw 'Asset import failed.' }
        & $godot --headless --path $project --export-release 'Web Probe' "$probe/site/TouhouSurvivor/index.html" 2>&1 | Tee-Object -FilePath "$probe/engine-export.log" -Encoding utf8NoBOM
        if ($LASTEXITCODE -ne 0) { throw 'Web export failed.' }
        if (Select-String -Path "$probe/engine-export.log" -Pattern '^ERROR:|error [A-Z]+[0-9]+:' -Quiet) { throw 'Exporter reported errors despite its exit code.' }
        if (-not (Test-Path "$probe/site/TouhouSurvivor/index.html")) { throw 'No Web entry point was produced.' }
        Write-Host 'WEB_PROBE_EXPORT_PASS'
    }
} finally { Pop-Location }
