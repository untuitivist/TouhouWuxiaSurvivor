param([string]$ProjectPath = '', [string]$Label = 'after', [string[]]$Scenes = @('performance', 'combat', 'reimu-spell', 'marisa-beam', 'pause'))
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!$ProjectPath) { $ProjectPath = $root }
$output = Join-Path $root ('artifacts/render-performance/' + $Label + '-' + [DateTime]::Now.ToString('yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force -Path $output | Out-Null
$godot = 'D:\_soft\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
foreach ($scene in $Scenes) {
    $stdout = Join-Path $output ($scene + '.log')
    $stderr = Join-Path $output ($scene + '.err.log')
    $capture = (Join-Path $output ($scene + '.png')).Replace('\', '/')
    Write-Host "Render check $Label / $scene"
    $process = Start-Process -FilePath $godot -ArgumentList @('--path', $ProjectPath, '--audio-driver', 'Dummy', '--', "--rebirth-screen=$scene", "--rebirth-capture=$capture") -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    if (!$process.WaitForExit(120000)) { Stop-Process -Id $process.Id; throw 'Render capture timeout' }
    $process.WaitForExit()
    Get-Content -LiteralPath $stdout
    Get-Content -LiteralPath $stderr -TotalCount 45
    if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $stdout -SimpleMatch 'REBIRTH_CAPTURE_PASS' -Quiet) -or (Select-String -LiteralPath $stderr -Pattern '^ERROR:|SHADER ERROR:' -Quiet)) { throw 'Render capture failed' }
}
[IO.File]::WriteAllText((Join-Path $root "artifacts/render-$Label-latest.txt"), $output, [Text.UTF8Encoding]::new($false))
Write-Host "RENDER_VALIDATION_PASS $output"
