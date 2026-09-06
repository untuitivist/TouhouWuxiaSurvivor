@echo off
setlocal
cd /d "%~dp0..\.."
set "STAGE=%~1"
set "VERSION=%~2"
if "%VERSION%"=="" set "VERSION=4.7.1"
if not "%VERSION%"=="4.7.1" if not "%VERSION%"=="4.6.1" exit /b 2
if not "%STAGE%"=="prepare" if not "%STAGE%"=="workload" if not "%STAGE%"=="export" exit /b 2
if not exist artifacts\web-probe-20260906 mkdir artifacts\web-probe-20260906
echo Web probe stage: %STAGE%, %VERSION%. Log: artifacts\web-probe-20260906\%STAGE%-%VERSION%.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe -NoProfile -File tools\web_probe\run.ps1 -Stage %STAGE% -EditorVersion %VERSION% > artifacts\web-probe-20260906\%STAGE%-%VERSION%.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\web-probe-20260906\%STAGE%-%VERSION%.log
exit /b %RESULT%
