@echo off
setlocal
cd /d "%~dp0..\.."
if not defined ASEPRITE_EXE set "ASEPRITE_EXE=D:\thesteam\steamapps\common\Aseprite\Aseprite.exe"
if not exist "%ASEPRITE_EXE%" exit /b 1
if not exist artifacts mkdir artifacts
"%ASEPRITE_EXE%" --batch --script-param "root=%CD%" --script tools/aseprite/draw_redraw.lua > artifacts/aseprite-redraw.log 2>&1
exit /b %errorlevel%
