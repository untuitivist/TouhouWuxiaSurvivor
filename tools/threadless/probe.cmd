@echo off
setlocal
cd /d "%~dp0..\.."
set "NODE_PATH=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules"
echo Testing real threadless C# startup without isolation. Log: artifacts\threadless-probe.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe tools\threadless\probe.cjs %* > artifacts\threadless-probe.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\threadless-probe.log
exit /b %RESULT%
