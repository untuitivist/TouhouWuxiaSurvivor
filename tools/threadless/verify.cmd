@echo off
setlocal
cd /d "%~dp0..\.."
set "NODE_PATH=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules"
echo Validating the shared game with and without isolation. Log: artifacts\threadless-validation.log
"C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe" tools\threadless\verify.cjs > artifacts\threadless-validation.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\threadless-validation.log
exit /b %RESULT%
