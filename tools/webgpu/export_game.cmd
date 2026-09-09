@echo off
setlocal
cd /d "%~dp0..\.."
set "EXPORT_LOG=artifacts\webgpu-engine-f329e39c\export-%RANDOM%-%RANDOM%.log"
echo Exporting the maintained C# game with the isolated WebGPU template. Log: %EXPORT_LOG%
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe -NoProfile -File tools\webgpu\export_game.ps1 > "%EXPORT_LOG%" 2>&1
set "RESULT=%ERRORLEVEL%"
type "%EXPORT_LOG%"
exit /b %RESULT%
