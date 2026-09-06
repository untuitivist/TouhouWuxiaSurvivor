@echo off
setlocal
cd /d "%~dp0..\.."
set "NODE_PATH=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules"
echo Verifying loading UX. Log: artifacts\loading-verify.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe tools\platform\verify_loading.cjs > artifacts\loading-verify.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\loading-verify.log
exit /b %RESULT%
