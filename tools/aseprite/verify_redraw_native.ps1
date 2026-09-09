param()
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$encoding = [Text.UTF8Encoding]::new($false)
$output = Join-Path $root ('artifacts/redraw-native/' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
$stage = Join-Path $output 'stage'
New-Item -ItemType Directory -Path $stage -Force | Out-Null
foreach ($relative in @('game', 'assets/aseprite/shrine-v04', 'assets/fonts', 'assets/internal_original/base/audio', 'project.godot', 'export_presets.cfg', 'TouhouWuxiaSurvivor.csproj', 'TouhouWuxiaSurvivor.sln', 'CHANGELOG.md')) {
    $target = Join-Path $stage $relative
    New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $root $relative) -Destination $target -Recurse
}
$godot = 'D:/_soft/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64_console.exe'
Push-Location $stage
try {
    & dotnet build TouhouWuxiaSurvivor.csproj --configuration Debug
    if ($LASTEXITCODE -ne 0) { throw 'Native shared build failed' }
    & $godot --headless --path $stage --editor --import
    if ($LASTEXITCODE -ne 0) { throw 'Native redraw import failed' }
} finally { Pop-Location }
$checks = [Collections.Generic.List[string]]::new()
function Test-Native([string]$Name, [string[]]$Arguments, [string]$Expected) {
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $godot
    $start.WorkingDirectory = $stage
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.StandardOutputEncoding = $encoding
    $start.StandardErrorEncoding = $encoding
    foreach ($argument in @('--path', $stage, '--audio-driver', 'Dummy') + $Arguments) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (!$process.WaitForExit(120000)) { $process.Kill($true); throw "Timeout: $Name" }
    $text = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
    [IO.File]::WriteAllText((Join-Path $output "$Name.log"), $text, $encoding)
    if ($process.ExitCode -ne 0 -or !$text.Contains($Expected) -or $text -match '(?m)^ERROR:') { throw "Failed $Name : $text" }
    $process.Dispose()
    $checks.Add($Name)
    Write-Host "REDRAW_NATIVE_PASS $Name"
}
Test-Native 'smoke' @('--headless', '--', '--rebirth-smoke') 'REBIRTH_UI_SMOKE_PASS'
Test-Native 'language' @('--headless', '--', '--rebirth-language-smoke') 'LANGUAGE_SMOKE_PASS'
foreach ($screen in @('title', 'heroes', 'settings', 'journal', 'journal-detail', 'reimu-field', 'reimu-spell', 'marisa-stars', 'marisa-warmup', 'marisa-beam', 'boss')) {
    Test-Native $screen @('--resolution', '1280x720', '--', "--rebirth-screen=$screen", "--rebirth-capture=$output/$screen.png") 'REBIRTH_CAPTURE_PASS'
}
Test-Native 'batch-reimu' @('--', '--rebirth-batch-smoke', "--rebirth-batch-output=$output") 'SPRITE_BATCH_VISUAL_PASS'
Test-Native 'batch-marisa' @('--', '--rebirth-batch-smoke', '--rebirth-batch-marisa', "--rebirth-batch-output=$output") 'SPRITE_BATCH_VISUAL_PASS'
[IO.File]::WriteAllText("$output/report.json", (@{ passed=$true; checks=$checks; stage=$stage; sourceCommit=(& git -C $root rev-parse HEAD).Trim(); sourceDirty=[bool](& git -C $root status --porcelain) } | ConvertTo-Json -Depth 4), $encoding)
[IO.File]::WriteAllText("$root/artifacts/redraw-native-latest.json", (@{ output=$output; report="$output/report.json" } | ConvertTo-Json), $encoding)
Write-Host "REDRAW_NATIVE_COMPLETE $output"
