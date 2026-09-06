@echo off
setlocal
cd /d "%~dp0..\.."
if not exist artifacts mkdir artifacts
echo Preparing the licensed shared font. Log: artifacts\font-build.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe -NoProfile -File tools\platform\prepare_font.ps1 > artifacts\font-build.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\font-build.log
exit /b %RESULT%
