@echo off
setlocal
cd /d "%~dp0..\.."
set "NODE_PATH=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules"
if not "%~1"=="" set "TOUHOU_VERIFY_OUTPUT=%~1"
if not "%~2"=="" set "TOUHOU_VERIFY_BUILD=%~2"
echo Verifying Marisa growth. Log: artifacts\marisa-growth-verify.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe tools\platform\verify_marisa_growth.cjs > artifacts\marisa-growth-verify.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\marisa-growth-verify.log
exit /b %RESULT%
