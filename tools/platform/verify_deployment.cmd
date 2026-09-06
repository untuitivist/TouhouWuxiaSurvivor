@echo off
setlocal
cd /d "%~dp0..\.."
set "NODE_PATH=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules"
echo Public verification log: artifacts\deployment\verify.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe tools\platform\verify_deployment.cjs > artifacts\deployment\verify.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\deployment\verify.log
exit /b %RESULT%
