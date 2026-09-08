@echo off
setlocal
cd /d "%~dp0..\.."
if not exist artifacts mkdir artifacts
set "NODE_PATH=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules"
echo Checking card journal in Web. Log: artifacts\journal-web.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe tools\platform\verify_journal.cjs > artifacts\journal-web.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\journal-web.log
exit /b %RESULT%
