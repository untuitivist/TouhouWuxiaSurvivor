@echo off
setlocal
cd /d "%~dp0..\.."
set "NODE_PATH=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules"
echo Checking Web renderer and high-DPI touch layout. Log: artifacts\web-performance.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe tools\platform\verify_performance.cjs > artifacts\web-performance.log 2>&1
if errorlevel 1 goto failed
type artifacts\web-performance.log
exit /b 0
:failed
type artifacts\web-performance.log
exit /b 1
