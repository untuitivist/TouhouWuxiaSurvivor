@echo off
setlocal
cd /d "%~dp0..\.."
if not defined ASEPRITE_EXE set "ASEPRITE_EXE=D:\thesteam\steamapps\common\Aseprite\Aseprite.exe"
if not exist artifacts mkdir artifacts
"%ASEPRITE_EXE%" --batch --script-param "root=%CD%" --script tools/aseprite/draw_character_study.lua > artifacts/character-study.log 2>&1
set "DRAW_RESULT=%errorlevel%"
type artifacts\character-study.log
exit /b %DRAW_RESULT%
