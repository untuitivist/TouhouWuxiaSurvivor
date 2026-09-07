@echo off
setlocal
cd /d "%~dp0..\.."
set "NODE_PATH=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules"
echo Checking original hero art in Web. Log: artifacts\hero-art-web.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe tools\platform\verify_hero_art.cjs > artifacts\hero-art-web.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\hero-art-web.log
exit /b %RESULT%
