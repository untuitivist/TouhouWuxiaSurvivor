@echo off
setlocal
cd /d "%~dp0..\.."
if not defined NODE_PATH set "NODE_PATH=C:/Users/untuitivist/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules"
set "NODE_EXE=C:/Users/untuitivist/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe"
if not exist "%NODE_EXE%" exit /b 1
"%NODE_EXE%" tools/aseprite/verify_redraw.cjs
exit /b %errorlevel%
