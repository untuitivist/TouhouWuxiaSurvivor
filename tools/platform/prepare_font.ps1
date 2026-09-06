$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$cache = Join-Path $root 'artifacts/font-source'
$fonts = Join-Path $root 'assets/fonts'
New-Item -ItemType Directory -Force -Path $cache, $fonts | Out-Null
$source = Join-Path $cache 'NotoSansCJKsc-Regular.otf'
if (-not (Test-Path $source)) {
    Invoke-WebRequest 'https://raw.githubusercontent.com/notofonts/noto-cjk/Sans2.004/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Regular.otf' -OutFile $source
}
if (-not (Test-Path "$fonts/OFL.txt")) {
    $reply = Invoke-WebRequest 'https://raw.githubusercontent.com/notofonts/noto-cjk/Sans2.004/LICENSE'
    $text = if ($reply.Content -is [byte[]]) { [Text.Encoding]::UTF8.GetString($reply.Content) } else { [string]$reply.Content }
    [IO.File]::WriteAllText("$fonts/OFL.txt", $text, [Text.UTF8Encoding]::new($false))
}
if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne '2c76254f6fc379fddfce0a7e84fb5385bb135d3e399294f6eeb6680d0365b74b') {
    throw 'Upstream font checksum mismatch; cached file preserved for inspection.'
}
& 'C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' "$PSScriptRoot/subset_font.py"
if ($LASTEXITCODE -ne 0) { throw 'Font preparation failed.' }
