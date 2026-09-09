@echo off
setlocal
cd /d "%~dp0..\.."
set "NODE_PATH=C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules"
if not exist artifacts\webgpu-game mkdir artifacts\webgpu-game
set "GAME_LOG=artifacts\webgpu-game\probe-%RANDOM%-%RANDOM%.log"
echo Verifying the actual C# game. Log: %GAME_LOG%
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe tools\webgpu\game_probe.cjs %* > "%GAME_LOG%" 2>&1
set "RESULT=%ERRORLEVEL%"
type "%GAME_LOG%"
exit /b %RESULT%
