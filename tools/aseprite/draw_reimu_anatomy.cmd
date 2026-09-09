@echo off
setlocal
cd /d "%~dp0..\.."
if not defined ASEPRITE_EXE set "ASEPRITE_EXE=D:\thesteam\steamapps\common\Aseprite\Aseprite.exe"
if not defined STUDY_OUTPUT set "STUDY_OUTPUT=%CD%\art\characters\reimu\study-02"
if exist "%STUDY_OUTPUT%\reimu.aseprite" (
    echo Refusing to overwrite existing editable source. Set STUDY_OUTPUT to a new directory.
    exit /b 1
)
if not exist artifacts mkdir artifacts
"%ASEPRITE_EXE%" --batch --script-param "root=%CD%" --script-param "output=%STUDY_OUTPUT%" --script tools/aseprite/draw_reimu_anatomy.lua > artifacts/reimu-anatomy.log 2>&1
set "DRAW_RESULT=%errorlevel%"
type artifacts\reimu-anatomy.log
if not "%DRAW_RESULT%"=="0" exit /b %DRAW_RESULT%
findstr /b /c:"REIMU_ANATOMY_STUDY_PASS " artifacts\reimu-anatomy.log >nul
if errorlevel 1 exit /b 1
if not exist "%STUDY_OUTPUT%\comparison.aseprite" exit /b 1
exit /b 0
