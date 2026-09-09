$ErrorActionPreference = 'Stop'
$encoding = [Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = $encoding
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$pointer = Join-Path $root 'artifacts/webgpu-engine-f329e39c/latest-output.txt'
$linuxOutput = [IO.File]::ReadAllText($pointer, $encoding).Trim()
$drivePrefix = '/mnt/' + $root.Substring(0, 1).ToLowerInvariant() + '/'
if (!$linuxOutput.StartsWith($drivePrefix, [StringComparison]::Ordinal)) { throw 'Unexpected integrated output mount.' }
$output = [IO.Path]::GetFullPath($root.Substring(0, 2) + '/' + $linuxOutput.Substring($drivePrefix.Length))
$allowed = [IO.Path]::GetFullPath((Join-Path $root 'artifacts/webgpu-engine-f329e39c/output')) + [IO.Path]::DirectorySeparatorChar
if (!$output.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase)) { throw 'Integrated template output escapes the experiment directory.' }
$template = Join-Path $output 'godot.web.template_release.wasm32.nothreads.mono.zip'
$preparation = [IO.File]::ReadAllText((Join-Path $root 'artifacts/webgpu-engine-f329e39c/preparation.json'), $encoding) | ConvertFrom-Json
if (!$preparation.passed -or 'mono' -notin $preparation.supported -or 'webgpu' -notin $preparation.supported) { throw 'Engine preparation did not pass.' }
& "$root/tools/platform/build_web.ps1" -Threadless -ExperimentalWebGpuTemplate $template
