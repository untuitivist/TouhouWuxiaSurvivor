param([Parameter(Mandatory)][string]$Root)
$ErrorActionPreference = 'Stop'
$cache = Join-Path $Root 'artifacts/threadless-toolchain'
$template = Join-Path $cache 'output/godot.web.template_release.wasm32.nothreads.mono.zip'
$feed = Join-Path $cache 'output/nuget'
if (!(Test-Path -LiteralPath $template) -or !(Test-Path -LiteralPath "$feed/Godot.NET.Sdk.4.6.1.nupkg")) { throw 'Build the isolated threadless toolchain first: tools/threadless/build.cmd' }
$original = Join-Path $Root 'artifacts/web-probe-20260906/editor-4.6.1/Godot_v4.6.1-stable_mono_web_export_win64'
$editor = Join-Path $cache 'editor'
New-Item -ItemType Directory -Force -Path $editor | Out-Null
foreach ($name in @('Godot_v4.6.1-stable_mono_web_export_win64.exe', 'Godot_v4.6.1-stable_mono_web_export_win64_console.exe', 'GodotSharp')) {
    if (!(Test-Path -LiteralPath "$editor/$name")) { Copy-Item -LiteralPath "$original/$name" -Destination $editor -Recurse }
}
$encoding = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText("$editor/_sc_", '', $encoding)
$templates = Join-Path $editor 'editor_data/export_templates/4.6.1.stable.mono'
New-Item -ItemType Directory -Force -Path $templates | Out-Null
Copy-Item -LiteralPath $template -Destination "$templates/web_nothreads_release.zip" -Force
return @{ Editor=$editor; Feed=$feed; Cache=$cache; Template=$template }
