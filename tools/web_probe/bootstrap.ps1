param([string]$ProbeDirectory = 'artifacts/web-probe-20260906', [ValidateSet('4.7.1', '4.6.1')][string]$EditorVersion = '4.7.1')

$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$probe = [IO.Path]::GetFullPath((Join-Path $repository $ProbeDirectory))
if (-not $probe.StartsWith($repository + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Probe directory must remain inside the repository.'
}
$downloads = Join-Path $probe 'downloads'
New-Item -ItemType Directory -Force -Path $downloads | Out-Null
$encoding = [Text.UTF8Encoding]::new($false)

function Get-VerifiedArchive([string]$Url, [string]$FileName, [string]$Algorithm, [string]$Digest) {
    $destination = Join-Path $downloads $FileName
    $verified = (Test-Path -LiteralPath $destination) -and ((Get-FileHash -LiteralPath $destination -Algorithm $Algorithm).Hash -eq $Digest)
    if (-not $verified) {
        Write-Host "Downloading $FileName"
        $proxyArguments = @()
        $address = [Uri]$Url
        if (-not [Net.Http.HttpClient]::DefaultProxy.IsBypassed($address)) {
            $proxyArguments = @('--proxy', [Net.Http.HttpClient]::DefaultProxy.GetProxy($address).AbsoluteUri)
        }
        & curl.exe @proxyArguments --fail --location --retry 2 --connect-timeout 30 --max-time 600 --speed-time 30 --speed-limit 1024 --continue-at - --output $destination $Url
        if ($LASTEXITCODE -ne 0) { throw "Download failed: $FileName" }
    }
    if ((Get-FileHash -LiteralPath $destination -Algorithm $Algorithm).Hash -ne $Digest) {
        throw "Hash mismatch: $destination. File preserved for inspection."
    }
    Write-Host "Verified $Algorithm $FileName"
    return $destination
}

$digests = @{ '4.7.1' = 'ad76e72610187b13e83229e863928c32689b1ba5dda34f5210940d563b89e473'; '4.6.1' = 'fa0e3d834864eb135c599b28c2edda942dfc2b033320a388262febe20e49097e' }
$editorArchive = Get-VerifiedArchive `
    "https://github.com/ComplexRobot/godot-dotnet-web-export/releases/download/$EditorVersion-stable/Godot_v$EditorVersion-stable_mono_web_export_win64.zip" `
    "godot-web-$EditorVersion.zip" 'SHA256' $digests[$EditorVersion]
$sdkArchive = Get-VerifiedArchive `
    'https://builds.dotnet.microsoft.com/dotnet/Sdk/9.0.317/dotnet-sdk-9.0.317-win-x64.zip' `
    'dotnet-sdk-9.0.317.zip' 'SHA512' '9d2206253b14bdad493e08b5d843d5da1bd534f66d7a43ca1ac4fed31fd11721f5f7bfc8fa79cc3ba0370a705a0c4e7226c1ce214489d239a55580d864e9e43a'

$editorDirectory = if ($EditorVersion -eq '4.7.1') { 'editor' } else { "editor-$EditorVersion" }
foreach ($entry in @(@{ Archive = $editorArchive; Directory = $editorDirectory }, @{ Archive = $sdkArchive; Directory = 'dotnet' })) {
    $destination = Join-Path $probe $entry.Directory
    $marker = Join-Path $destination '.probe-extracted'
    if (-not (Test-Path -LiteralPath $marker)) {
        New-Item -ItemType Directory -Force -Path $destination | Out-Null
        Expand-Archive -LiteralPath $entry.Archive -DestinationPath $destination -Force
        [IO.File]::WriteAllText($marker, 'Verified archive extracted.', $encoding)
    }
}

Write-Host 'Isolated editor contents:'
Get-ChildItem (Join-Path $probe $editorDirectory) -Recurse -File | Where-Object {
    $_.Extension -in '.exe', '.bat', '.nupkg', '.zip'
} | Select-Object FullName, Length | Format-Table -AutoSize
Write-Host 'Isolated SDK:'
& (Join-Path $probe 'dotnet/dotnet.exe') --list-sdks
if ($LASTEXITCODE -ne 0) { throw 'Isolated SDK failed.' }
