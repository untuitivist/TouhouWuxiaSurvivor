@echo off
setlocal
cd /d "%~dp0..\.."
set "NODE_PATH=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules"
echo Checking original scenery in Web. Log: artifacts\scenery-art-web.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe tools\platform\verify_scenery_art.cjs > artifacts\scenery-art-web.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\scenery-art-web.log
exit /b %RESULT%
