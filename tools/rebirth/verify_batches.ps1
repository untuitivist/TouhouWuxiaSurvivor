param()
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$encoding = [Text.UTF8Encoding]::new($false)
$output = Join-Path $root ('artifacts/batch-render/' + [DateTime]::Now.ToString('yyyyMMdd-HHmmss'))
[IO.Directory]::CreateDirectory($output) | Out-Null
foreach ($hero in @('reimu', 'marisa')) {
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = 'D:\_soft\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
    $start.WorkingDirectory = $root
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.StandardOutputEncoding = $encoding
    $start.StandardErrorEncoding = $encoding
    foreach ($argument in @('--path', $root, '--audio-driver', 'Dummy', '--', '--rebirth-batch-smoke')) { $start.ArgumentList.Add($argument) }
    if ($hero -eq 'marisa') { $start.ArgumentList.Add('--rebirth-batch-marisa') }
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (!$process.WaitForExit(120000)) { $process.Kill($true); throw "Batch visual check timed out: $hero" }
    $text = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
    [IO.File]::WriteAllText((Join-Path $output "$hero.log"), $text, $encoding)
    if ($process.ExitCode -ne 0 -or $text -notmatch 'SPRITE_BATCH_VISUAL_PASS.+checks=81' -or $text -match '(?m)^ERROR:|SHADER ERROR:') { throw "Batch rendering failed: $output/$hero.log" }
    if ([regex]::Matches($text, 'BATTLE_BATCH_CHECK').Count -ne 12) { throw 'Missing full-battle comparisons' }
    if ([regex]::Matches($text, 'BATCH_COLOR_CHECK').Count -ne 24 -or $text -notmatch 'BATCH_COLOR_VISUAL_PASS checks=24') { throw 'Missing interleaved color comparisons' }
    Write-Output "BATCH_RENDER_PASS $hero $output"
    $process.Dispose()
}
[IO.File]::WriteAllText((Join-Path $root 'artifacts/batch-render-latest.txt'), $output, $encoding)
