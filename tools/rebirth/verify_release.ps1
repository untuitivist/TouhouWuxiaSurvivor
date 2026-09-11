param([string]$Executable = '', [string]$OutputDirectory = '', [string]$ArtifactSourceCommit = '')

$ErrorActionPreference = 'Stop'
$encoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = $encoding
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$project = [System.IO.File]::ReadAllText((Join-Path $root 'project.godot'), $encoding)
$versionMatch = [regex]::Match($project, '(?m)^config/version="((alpha|beta|rc|stable)-(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*))"\r?$')
if (-not $versionMatch.Success) { throw 'Project version does not follow stage-major.release.optimization.' }
$version = $versionMatch.Groups[1].Value
$numericVersion = '{0}.{1}.{2}.0' -f $versionMatch.Groups[3].Value, $versionMatch.Groups[4].Value, $versionMatch.Groups[5].Value
if (-not $Executable) { $Executable = Join-Path $root "release/TouhouWuxiaSurvivor_$version.exe" }
$source = Get-Item -LiteralPath $Executable
if ($source.VersionInfo.FileVersion -ne $numericVersion -or $source.VersionInfo.ProductVersion -ne $numericVersion) { throw 'Windows version metadata mismatch.' }
$changelog = [System.IO.File]::ReadAllText((Join-Path $root 'CHANGELOG.md'), $encoding)
if ([regex]::Match($changelog, '(?m)^## ([a-z]+-\d+\.\d+\.\d+)').Groups[1].Value -ne $version) { throw 'Changelog version mismatch.' }
$configurationPath = Join-Path $root '.godot/mono/temp/bin/ExportRelease/win-x64/TouhouWuxiaSurvivor.runtimeconfig.json'
$configuration = [System.IO.File]::ReadAllText($configurationPath, $encoding) | ConvertFrom-Json
if (-not $configuration.runtimeOptions.includedFrameworks) { throw 'Export publish output is not self-contained.' }
$isolated = Join-Path ([System.IO.Path]::GetTempPath()) ("TouhouWuxiaSurvivor-$version-" + [Guid]::NewGuid().ToString('N'))
$logs = if ($OutputDirectory) { [System.IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $root "artifacts/$version-export-validation" }
[System.IO.Directory]::CreateDirectory($isolated) | Out-Null
[System.IO.Directory]::CreateDirectory($logs) | Out-Null
$portable = Join-Path $isolated $source.Name
Copy-Item -LiteralPath $source.FullName -Destination $portable
if (@(Get-ChildItem -LiteralPath $isolated -Force).Count -ne 1) { throw 'Standalone directory must contain only the EXE before launch.' }

function Invoke-ReleaseCheck([string]$Name, [string[]]$GameArguments, [string]$Expected) {
    $start = [System.Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $portable
    $start.WorkingDirectory = $isolated
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.StandardOutputEncoding = $encoding
    $start.StandardErrorEncoding = $encoding
    $start.Environment['PATH'] = "$env:SystemRoot\System32;$env:SystemRoot"
    $start.Environment['DOTNET_ROOT'] = Join-Path $isolated 'no-global-dotnet'
    $start.Environment['DOTNET_ROOT_X64'] = $start.Environment['DOTNET_ROOT']
    $start.Environment['DOTNET_MULTILEVEL_LOOKUP'] = '0'
    $start.ArgumentList.Add('--log-file')
    $start.ArgumentList.Add((Join-Path $logs "$Name-engine.log"))
    foreach ($argument in $GameArguments) { $start.ArgumentList.Add($argument) }
    $process = [System.Diagnostics.Process]::Start($start)
    $output = $process.StandardOutput.ReadToEndAsync()
    $errors = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(90000)) { $process.Kill($true); throw "$Name timed out." }
    $text = $output.GetAwaiter().GetResult() + $errors.GetAwaiter().GetResult()
    [System.IO.File]::WriteAllText((Join-Path $logs "$Name.log"), $text, $encoding)
    if ($process.ExitCode -ne 0 -or -not $text.Contains($Expected) -or $text -match '(?m)^ERROR:') { throw "$Name failed: $text" }
    Write-Output "PASS $Name"
    $process.Dispose()
}

Invoke-ReleaseCheck 'standalone-smoke' @('--headless', '--', '--rebirth-smoke') 'REBIRTH_UI_SMOKE_PASS'
Invoke-ReleaseCheck 'standalone-language' @('--headless', '--', '--rebirth-language-smoke') 'LANGUAGE_SMOKE_PASS'
Invoke-ReleaseCheck 'standalone-language-settings' @('--resolution', '960x540', '--audio-driver', 'Dummy', '--', '--rebirth-language=en', '--rebirth-screen=settings', "--rebirth-capture=$(Join-Path $logs 'language-settings.png')") 'REBIRTH_CAPTURE_PASS'
Invoke-ReleaseCheck 'standalone-display' @('--audio-driver', 'Dummy', '--', '--rebirth-video-smoke') 'REBIRTH_DISPLAY_PASS'
Invoke-ReleaseCheck 'standalone-title' @('--audio-driver', 'Dummy', '--', '--rebirth-screen=title', "--rebirth-capture=$(Join-Path $logs 'title.png')") 'REBIRTH_CAPTURE_PASS'
Invoke-ReleaseCheck 'standalone-boss' @('--audio-driver', 'Dummy', '--', '--rebirth-screen=boss', "--rebirth-capture=$(Join-Path $logs 'boss.png')") 'REBIRTH_CAPTURE_PASS'
$screens = @('heroes', 'build', 'settings', 'settings-video', 'settings-controls', 'settings-confirm', 'debug-title', 'debug-combat', 'changelog', 'journal', 'journal-detail', 'reimu-field', 'reimu-spell', 'marisa-stars', 'marisa-warmup', 'marisa-beam', 'marisa-build', 'marisa-choices')
foreach ($screen in $screens) {
    Invoke-ReleaseCheck "standalone-$screen" @('--resolution', '960x540', '--audio-driver', 'Dummy', '--', "--rebirth-screen=$screen", "--rebirth-capture=$(Join-Path $logs "$screen.png")") 'REBIRTH_CAPTURE_PASS'
}
foreach ($screen in @('marisa-growth-choices', 'marisa-growth-combat', 'marisa-growth-build')) {
    Invoke-ReleaseCheck "standalone-$screen" @('--resolution', '960x540', '--audio-driver', 'Dummy', '--', "--rebirth-screen=$screen", "--rebirth-capture=$(Join-Path $logs "$screen.png")") 'REBIRTH_CAPTURE_PASS'
}
Invoke-ReleaseCheck 'standalone-controls-small' @('--resolution', '640x360', '--audio-driver', 'Dummy', '--', '--rebirth-screen=settings-controls', "--rebirth-capture=$(Join-Path $logs 'controls-small.png')") 'REBIRTH_CAPTURE_PASS'
Invoke-ReleaseCheck 'standalone-heroes-small' @('--resolution', '640x360', '--audio-driver', 'Dummy', '--', '--rebirth-language=en', '--rebirth-screen=heroes', "--rebirth-capture=$(Join-Path $logs 'heroes-small.png')") 'REBIRTH_CAPTURE_PASS'
foreach ($hero in @('reimu', 'marisa')) {
    $arguments = @('--audio-driver', 'Dummy', '--', '--rebirth-batch-smoke', "--rebirth-batch-output=$logs")
    if ($hero -eq 'marisa') { $arguments += '--rebirth-batch-marisa' }
    Invoke-ReleaseCheck "standalone-batch-$hero" $arguments 'SPRITE_BATCH_VISUAL_PASS'
    $batchLog = [IO.File]::ReadAllText((Join-Path $logs "standalone-batch-$hero.log"), $encoding)
    if ($batchLog -notmatch 'SPRITE_BATCH_VISUAL_PASS.+checks=81' -or [regex]::Matches($batchLog, 'BATTLE_BATCH_CHECK').Count -ne 12) { throw 'Incomplete standalone batch verification' }
    if ($batchLog -notmatch 'BATCH_COLOR_VISUAL_PASS checks=24' -or [regex]::Matches($batchLog, 'BATCH_COLOR_CHECK').Count -ne 24) { throw 'Incomplete standalone color verification' }
}
if (@(Get-ChildItem -LiteralPath $isolated -Force).Count -ne 1) { throw 'Standalone directory must still contain only the EXE after all checks.' }
$checksum = (Get-FileHash -LiteralPath $source.FullName -Algorithm SHA256).Hash
$verificationCommit = (& git -C $root rev-parse HEAD).Trim()
$report = [ordered]@{
    source_commit = if ($ArtifactSourceCommit) { $ArtifactSourceCommit } else { $verificationCommit }
    verification_commit = $verificationCommit
    version = $version
    windows_version = $numericVersion
    executable = $source.FullName
    bytes = $source.Length
    sha256 = $checksum
    isolated_directory = $isolated
    isolated_files = @(Get-ChildItem -LiteralPath $isolated -Force | ForEach-Object { $_.Name })
    embedded_runtime = $configuration.runtimeOptions.includedFrameworks
    checks = @('standalone-smoke', 'standalone-language', 'standalone-language-settings', 'standalone-display', 'standalone-title', 'standalone-boss') + @($screens | ForEach-Object { "standalone-$_" }) + @('standalone-controls-small', 'standalone-heroes-small', 'standalone-batch-reimu', 'standalone-batch-marisa')
}
[System.IO.File]::WriteAllText((Join-Path $logs 'report.json'), ($report | ConvertTo-Json -Depth 5), $encoding)
Write-Output "SINGLE_EXE_VALIDATION_PASS version=$version bytes=$($source.Length) sha256=$checksum"
Write-Output "IsolatedDirectory=$isolated"
