@echo off
setlocal
cd /d "%~dp0..\.."
set "NODE_PATH=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules"
echo Verifying shared Web build. Log: artifacts\web-verify.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe tools\platform\verify_web.cjs %* > artifacts\web-verify.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\web-verify.log
exit /b %RESULT%
