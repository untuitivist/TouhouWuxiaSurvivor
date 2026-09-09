@echo off
setlocal
cd /d "%~dp0..\.."
set "NODE_EXE=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe"
set "NODE_PATH=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules"
if not exist artifacts\webgpu-lab mkdir artifacts\webgpu-lab
set "LAB_LOG=artifacts\webgpu-lab\verify-%RANDOM%-%RANDOM%.log"
echo Local rendering-only WebGPU experiment. Log: %LAB_LOG%
call :checks > "%LAB_LOG%" 2>&1
set "RESULT=%ERRORLEVEL%"
type "%LAB_LOG%"
exit /b %RESULT%
:checks
"%NODE_EXE%" --test tools\webgpu\scene.test.mjs tools\webgpu\server.test.cjs
if errorlevel 1 exit /b 1
"%NODE_EXE%" tools\webgpu\verify.cjs
if errorlevel 1 exit /b 1
exit /b 0
