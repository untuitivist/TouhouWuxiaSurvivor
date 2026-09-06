@echo off
setlocal
cd /d "%~dp0..\.."
echo Checking real renderer. Log: artifacts\render-verify.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe -NoProfile -File tools\rebirth\verify_render.ps1 %* > artifacts\render-verify.log 2>&1
if errorlevel 1 goto failed
type artifacts\render-verify.log
exit /b 0
:failed
type artifacts\render-verify.log
exit /b 1
