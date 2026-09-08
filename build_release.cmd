@echo off
setlocal

if not defined GODOT_EXE set "GODOT_EXE=D:\_soft\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe"
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
if exist "%OUTPUT_EXE%" (
    echo Existing release is preserved: %OUTPUT_EXE%
    echo Update the project version before exporting another release.
    exit /b 1
)
cd /d "%~dp0"
if not exist artifacts mkdir artifacts
call tools\aseprite\verify_redraw.cmd > artifacts\release-art-verification.log 2>&1
if errorlevel 1 exit /b 1
set DOTNET_CLI_UI_LANGUAGE=en
set DOTNET_CLI_USE_MSBUILD_SERVER=0
set UseSharedCompilation=false
echo Exporting %GAME_VERSION%. Log: artifacts\export-engine.log
"%GODOT_EXE%" --headless --path . --log-file artifacts\export-engine.log --export-release "Windows Release" "%OUTPUT_EXE%"
if errorlevel 1 exit /b %errorlevel%
if not exist "%OUTPUT_EXE%" exit /b 1

echo Release exported: %OUTPUT_EXE%
endlocal
exit /b 0
