param([switch]$InstallLinuxDependencies)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$cache = Join-Path $root 'artifacts/threadless-toolchain'
$encoding = [Text.UTF8Encoding]::new($false)
New-Item -ItemType Directory -Force -Path $cache | Out-Null
$configuration = Get-Content -LiteralPath "$PSScriptRoot/toolchain.json" -Raw | ConvertFrom-Json
$records = [Collections.Generic.List[object]]::new()
foreach ($download in $configuration.downloads) {
    $destination = Join-Path $cache $download.name
    if (!(Test-Path -LiteralPath $destination)) {
        $partial = $destination + '.partial'
        $proxyArguments = @()
        $address = [Uri]$download.url
        if (![Net.Http.HttpClient]::DefaultProxy.IsBypassed($address)) { $proxyArguments = @('--proxy', [Net.Http.HttpClient]::DefaultProxy.GetProxy($address).AbsoluteUri) }
        Write-Host "Downloading $($download.name) into isolated toolchain cache"
        & curl.exe @proxyArguments --fail --location --retry 2 --connect-timeout 30 --max-time 1200 --speed-time 60 --speed-limit 1024 --output $partial $download.url
        if ($LASTEXITCODE -ne 0) { throw "Download failed; partial preserved: $partial" }
        if ($download.hash -and (Get-FileHash -LiteralPath $partial -Algorithm $download.algorithm).Hash -ne $download.hash) { throw "Download digest mismatch: $partial" }
        Move-Item -LiteralPath $partial -Destination $destination
    }
    if ($download.hash -and (Get-FileHash -LiteralPath $destination -Algorithm $download.algorithm).Hash -ne $download.hash) { throw "Cached digest mismatch: $destination" }
    $records.Add(@{ name=$download.name; url=$download.url; bytes=(Get-Item -LiteralPath $destination).Length; sha256=(Get-FileHash -LiteralPath $destination).Hash })
}
[IO.File]::WriteAllText("$cache/downloads.json", ($records | ConvertTo-Json -Depth 4), $encoding)
$editor = Join-Path $root 'artifacts/web-probe-20260906/editor-4.6.1/Godot_v4.6.1-stable_mono_web_export_win64/Godot_v4.6.1-stable_mono_web_export_win64_console.exe'
if (!(Test-Path -LiteralPath $editor)) { throw 'The pinned 4.6.1 editor must already be bootstrapped.' }
if (!(Test-Path -LiteralPath "$cache/glue.ready")) {
    New-Item -ItemType Directory -Force -Path "$cache/glue" | Out-Null
    $process = Start-Process -FilePath $editor -WorkingDirectory $cache -ArgumentList @('--headless', '--quit', '--generate-mono-glue', "$cache/glue") -WindowStyle Hidden -PassThru -RedirectStandardOutput "$cache/glue.log" -RedirectStandardError "$cache/glue.err.log"
    $process.WaitForExit()
    Get-Content -LiteralPath "$cache/glue.log"
    Get-Content -LiteralPath "$cache/glue.err.log"
    if ($process.ExitCode -ne 0 -or !(Get-ChildItem "$cache/glue" -Filter '*.cs' -Recurse | Select-Object -First 1)) { throw 'Matching Mono glue generation failed.' }
    [IO.File]::WriteAllText("$cache/glue.ready", $configuration.engineRevision, $encoding)
}
$running = ((& wsl.exe --list --running --quiet) -join "`n") -replace "`0", ''
if ($running -match 'Ubuntu-22\.04') { throw 'Ubuntu-22.04 is already running; do not take ownership of another session.' }
$linuxRoot = '/mnt/' + $root.Substring(0, 1).ToLowerInvariant() + $root.Substring(2).Replace('\', '/')
$result = 1
try {
    if ($InstallLinuxDependencies) {
        & wsl.exe -d Ubuntu-22.04 -u root --cd / -- bash "$linuxRoot/tools/threadless/install_linux_dependencies.sh"
        if ($LASTEXITCODE -ne 0) { throw 'Linux dependency installation failed.' }
    }
    & wsl.exe -d Ubuntu-22.04 --cd / -- bash "$linuxRoot/tools/threadless/build.sh" $linuxRoot
    $result = $LASTEXITCODE
} finally { & wsl.exe --terminate Ubuntu-22.04 *> $null }
if ($result -ne 0) { throw "Isolated Linux build failed with exit code $result. Cache and logs are preserved." }
