@echo off
setlocal
cd /d "%~dp0..\.."
set "NODE_PATH=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules"
echo Checking Web sprite colors and batches. Log: artifacts\web-batch-render.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe tools\platform\verify_batches.cjs %* > artifacts\web-batch-render.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\web-batch-render.log
exit /b %RESULT%
