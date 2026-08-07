@echo off
setlocal

set "GODOT_EXE=D:\_soft\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe"
set "PROJECT_DIR=%~dp0."
set "OUTPUT_DIR=%~dp0release"
set "OUTPUT_EXE=%OUTPUT_DIR%\TouhouWuxiaSurvivor.exe"

if not exist "%GODOT_EXE%" (
    echo Godot executable not found: %GODOT_EXE%
    exit /b 1
)

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
"%GODOT_EXE%" --headless --quiet --path "%PROJECT_DIR%" --export-release "Windows Release" "%OUTPUT_EXE%"
if errorlevel 1 exit /b %errorlevel%

echo Release exported: %OUTPUT_EXE%
endlocal
