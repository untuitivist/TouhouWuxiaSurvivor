@echo off
setlocal

set "GODOT_EXE=D:\_soft\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe"
set "PROJECT_DIR=%~dp0."
set "OUTPUT_DIR=%~dp0release"

rem Read the only runtime version source and remove the surrounding Godot quotes.
for /f "tokens=2 delims==" %%V in ('findstr /b /c:"config/version=" "%PROJECT_DIR%\project.godot"') do set "GAME_VERSION=%%~V"
if not defined GAME_VERSION (
    echo Project version not found in project.godot.
    exit /b 1
)

set "OUTPUT_EXE=%OUTPUT_DIR%\TouhouWuxiaSurvivor_%GAME_VERSION%.exe"

if not exist "%GODOT_EXE%" (
    echo Godot executable not found: %GODOT_EXE%
    exit /b 1
)

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
"%GODOT_EXE%" --headless --quiet --path "%PROJECT_DIR%" --export-release "Windows Release" "%OUTPUT_EXE%"
if errorlevel 1 exit /b %errorlevel%

echo Release exported: %OUTPUT_EXE%
endlocal
