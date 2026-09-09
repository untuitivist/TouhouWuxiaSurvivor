@echo off
setlocal
cd /d "%~dp0..\.."
if not exist artifacts\webgpu-engine-f329e39c mkdir artifacts\webgpu-engine-f329e39c
set "ENGINE_LOG=artifacts\webgpu-engine-f329e39c\engine-%RANDOM%-%RANDOM%.log"
echo Isolated C# WebGPU engine verification. Log: %ENGINE_LOG%
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe -NoProfile -File tools\webgpu\full_engine.ps1 %* > "%ENGINE_LOG%" 2>&1
set "RESULT=%ERRORLEVEL%"
type "%ENGINE_LOG%"
exit /b %RESULT%
