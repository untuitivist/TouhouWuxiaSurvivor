@echo off
setlocal
cd /d "%~dp0..\.."
set "NODE_PATH=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules"
set "POWERSHELL7_EXE=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe"
if not exist artifacts\webgpu-validation mkdir artifacts\webgpu-validation
set "VALIDATION_LOG=artifacts\webgpu-validation\suite-%RANDOM%-%RANDOM%.log"
echo Full actual-game verification. Log: %VALIDATION_LOG%
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe tools\webgpu\full_validation.cjs %* > "%VALIDATION_LOG%" 2>&1
set "RESULT=%ERRORLEVEL%"
type "%VALIDATION_LOG%"
exit /b %RESULT%
