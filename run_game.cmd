@echo off
setlocal
if not defined GODOT_EXE set "GODOT_EXE=D:\_soft\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe"

if not exist "%GODOT_EXE%" (
    echo Godot executable not found: %GODOT_EXE%
    pause
    exit /b 1
)

cd /d "%~dp0"
if not exist artifacts mkdir artifacts
set DOTNET_CLI_UI_LANGUAGE=en
echo Building Rebirth runtime. Log: artifacts\launch-build.log
dotnet build TouhouWuxiaSurvivor.csproj --configuration Debug > artifacts\launch-build.log 2>&1
if errorlevel 1 (
    type artifacts\launch-build.log
    pause
    exit /b 1
)
"%GODOT_EXE%" --path . --log-file artifacts\game.log %*
exit /b %errorlevel%
