param([ValidateSet('prepare', 'build')][string]$Stage = 'prepare', [switch]$InstallHostCompiler)
$ErrorActionPreference = 'Stop'
$encoding = [Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = $encoding
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$node = 'C:/Users/untuitivist/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe'
& $node "$PSScriptRoot/engine_sources.cjs"
if ($LASTEXITCODE -ne 0) { throw 'Pinned engine source acquisition failed; all files are preserved.' }
$running = ((& wsl.exe --list --running --quiet) -join "`n") -replace "`0", ''
if ($running -match 'Ubuntu-22\.04') { throw 'Ubuntu-22.04 already belongs to another running session; not taking ownership.' }
$linuxRoot = '/mnt/' + $root.Substring(0, 1).ToLowerInvariant() + $root.Substring(2).Replace('\', '/')
$result = 1
try {
    if ($InstallHostCompiler) {
        & wsl.exe -d Ubuntu-22.04 -u root --cd / -- bash "$linuxRoot/tools/webgpu/install_host_compiler.sh"
        if ($LASTEXITCODE -ne 0) { throw 'Host compiler installation failed; no engine build attempted.' }
    }
    & wsl.exe -d Ubuntu-22.04 --cd / -- bash "$linuxRoot/tools/webgpu/engine_build.sh" $linuxRoot $Stage
    $result = $LASTEXITCODE
} finally { & wsl.exe --terminate Ubuntu-22.04 *> $null }
if ($result -ne 0) { throw "Isolated engine stage failed ($result). Cache and logs are preserved; game remains unchanged." }
Write-Output "ENGINE_STAGE_PASS $Stage"
