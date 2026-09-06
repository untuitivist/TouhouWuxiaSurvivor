@echo off
setlocal
cd /d "%~dp0..\.."
if not exist artifacts mkdir artifacts
echo Building isolated threadless C# Web toolchain. Log: artifacts\threadless-build.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe -NoProfile -File tools\threadless\build.ps1 %* > artifacts\threadless-build.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\threadless-build.log
exit /b %RESULT%
