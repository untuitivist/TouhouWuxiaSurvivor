@echo off
setlocal
cd /d "%~dp0..\.."
if not exist artifacts mkdir artifacts
echo Checking dynamic sprite batches. Log: artifacts\batch-render.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe -NoProfile -File tools\rebirth\verify_batches.ps1 > artifacts\batch-render.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\batch-render.log
exit /b %RESULT%
