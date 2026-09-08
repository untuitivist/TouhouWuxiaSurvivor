@echo off
setlocal
cd /d "%~dp0..\.."
if not defined ASEPRITE_EXE set "ASEPRITE_EXE=D:\thesteam\steamapps\common\Aseprite\Aseprite.exe"
if not exist "%ASEPRITE_EXE%" exit /b 1
if not exist artifacts mkdir artifacts
for %%F in (art\runtime\*.aseprite) do (
    "%ASEPRITE_EXE%" --batch "%%F" --save-as "assets\aseprite\%%~nF.png" >> artifacts\aseprite-runtime-export.log 2>&1
    if errorlevel 1 exit /b 1
)
echo ASEPRITE_RUNTIME_EXPORT_PASS
