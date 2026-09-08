param()
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$encoding = [Text.UTF8Encoding]::new($false)
$version = [regex]::Match([IO.File]::ReadAllText("$root/project.godot", $encoding), '(?m)^config/version="([A-Za-z0-9.-]+)"').Groups[1].Value
if (!$version) { throw 'Missing version' }
$commit = (& git -C $root rev-parse HEAD).Trim()
if (& git -C $root status --porcelain -- game assets project.godot export_presets.cfg TouhouWuxiaSurvivor.csproj CHANGELOG.md) { throw 'Windows release requires committed sources' }
$name = "TouhouWuxiaSurvivor_$version.exe"
$destination = Join-Path $root "release/$name"
if (Test-Path -LiteralPath $destination) { throw 'Historical executable already exists; do not overwrite it' }
$build = Join-Path $root ('artifacts/windows-builds/' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
$stage = Join-Path $build 'stage'
New-Item -ItemType Directory -Path "$stage/release", "$stage/tools/rebirth" -Force | Out-Null
$paths = @('game', 'assets/ui/title', 'assets/aseprite', 'assets/fonts', 'assets/internal_original/base', 'project.godot', 'export_presets.cfg', 'TouhouWuxiaSurvivor.csproj', 'TouhouWuxiaSurvivor.sln', 'CHANGELOG.md')
$manifest = [Collections.Generic.List[object]]::new()
foreach ($relative in $paths) {
    $source = Join-Path $root $relative
    $target = Join-Path $stage $relative
    New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $target -Recurse
    foreach ($file in Get-ChildItem -LiteralPath $source -File -Recurse | Where-Object { $_.Extension -notin @('.import', '.uid') }) {
        $manifest.Add(@{ path=[IO.Path]::GetRelativePath($root, $file.FullName); sha256=(Get-FileHash -LiteralPath $file.FullName).Hash })
    }
}
Copy-Item -LiteralPath "$root/tools/rebirth/verify_release.ps1" -Destination "$stage/tools/rebirth/verify_release.ps1"
$godot = 'D:/_soft/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64_console.exe'
Push-Location $stage
try {
    & dotnet build TouhouWuxiaSurvivor.csproj --configuration Debug
    if ($LASTEXITCODE -ne 0) { throw 'Native C# build failed' }
    & $godot --headless --path $stage --editor --import
    if ($LASTEXITCODE -ne 0) { throw 'Runtime asset import failed' }
    & $godot --headless --path $stage --export-release 'Windows Release' "$stage/release/$name" 2>&1 | Tee-Object -FilePath "$build/export.log" -Encoding utf8NoBOM
    if ($LASTEXITCODE -ne 0 -or (Select-String -LiteralPath "$build/export.log" -Pattern '^ERROR:|error [A-Z]+[0-9]+:' -Quiet)) { throw 'Windows export failed' }
    foreach ($file in $manifest) {
        if ((Get-FileHash -LiteralPath (Join-Path $root $file.path)).Hash -ne $file.sha256 -or (Get-FileHash -LiteralPath (Join-Path $stage $file.path)).Hash -ne $file.sha256) { throw "Source changed: $($file.path)" }
    }
    & "$stage/tools/rebirth/verify_release.ps1" -Executable "$stage/release/$name"
    if ($LASTEXITCODE -ne 0) { throw 'Standalone Windows verification failed' }
} finally { Pop-Location }
$validation = "$version-export-validation"
New-Item -ItemType Directory -Path "$root/artifacts/$validation" -Force | Out-Null
foreach ($file in Get-ChildItem -LiteralPath "$stage/artifacts/$validation") {
    Copy-Item -LiteralPath $file.FullName -Destination "$root/artifacts/$validation" -Recurse -Force
}
New-Item -ItemType Directory -Path "$root/release" -Force | Out-Null
Copy-Item -LiteralPath "$stage/release/$name" -Destination $destination
$reportPath = "$root/artifacts/$validation/report.json"
$report = [IO.File]::ReadAllText($reportPath, $encoding) | ConvertFrom-Json
if ((Get-FileHash -LiteralPath $destination).Hash -ne $report.sha256 -or $report.source_commit -ne $commit) { throw 'Published copy differs from verified source' }
$report.executable = $destination
[IO.File]::WriteAllText($reportPath, ($report | ConvertTo-Json -Depth 8), $encoding)
[IO.File]::WriteAllText("$build/source-manifest.json", (@{ sourceCommit=$commit; sourceFiles=$manifest; version=$version; executable=$destination } | ConvertTo-Json -Depth 6), $encoding)
Write-Output "WINDOWS_RELEASE_PASS $destination"
