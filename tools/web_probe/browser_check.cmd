@echo off
setlocal
cd /d "%~dp0..\.."
set "NODE_PATH=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules"
echo Running browser verification. Log: artifacts\web-probe-20260906\browser-check.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe tools\web_probe\browser_check.cjs > artifacts\web-probe-20260906\browser-check.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\web-probe-20260906\browser-check.log
exit /b %RESULT%
