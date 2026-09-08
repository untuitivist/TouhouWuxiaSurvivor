@echo off
setlocal
cd /d "%~dp0..\.."
if not defined ASEPRITE_EXE set "ASEPRITE_EXE=D:\thesteam\steamapps\common\Aseprite\Aseprite.exe"
if not exist "%ASEPRITE_EXE%" (
    echo Aseprite not found. Set ASEPRITE_EXE to your installed executable.
    exit /b 1
)
if not exist artifacts mkdir artifacts
"%ASEPRITE_EXE%" --batch art\title\moonlit_shrine.aseprite --save-as assets\ui\title\moonlit_shrine.png > artifacts\aseprite-title-export.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\aseprite-title-export.log
exit /b %RESULT%
