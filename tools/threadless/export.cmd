@echo off
setlocal
cd /d "%~dp0..\.."
if not exist artifacts mkdir artifacts
echo Exporting the shared C# game without threads. Log: artifacts\threadless-export.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe -NoProfile -File tools\platform\build_web.ps1 -Threadless > artifacts\threadless-export.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\threadless-export.log
exit /b %RESULT%
