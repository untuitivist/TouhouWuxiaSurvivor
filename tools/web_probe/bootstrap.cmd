@echo off
setlocal
cd /d "%~dp0..\.."
if not exist artifacts\web-probe-20260906 mkdir artifacts\web-probe-20260906
echo Downloading and checking isolated Web tools. Log: artifacts\web-probe-20260906\bootstrap.log
set "VERSION=%~1"
if "%VERSION%"=="" set "VERSION=4.7.1"
if not "%VERSION%"=="4.7.1" if not "%VERSION%"=="4.6.1" exit /b 2
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe -NoProfile -File tools\web_probe\bootstrap.ps1 -EditorVersion %VERSION% > artifacts\web-probe-20260906\bootstrap.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\web-probe-20260906\bootstrap.log
exit /b %RESULT%
