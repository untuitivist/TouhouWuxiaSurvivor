@echo off
setlocal
cd /d "%~dp0"
if not exist artifacts mkdir artifacts
echo Building Web from the shared project. Log: artifacts\web-build.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe -NoProfile -File tools\platform\build_web.ps1 %* > artifacts\web-build.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\web-build.log
exit /b %RESULT%
