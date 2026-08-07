@echo off
setlocal
set "GODOT_EXE=D:\_soft\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe"

if not exist "%GODOT_EXE%" (
    echo Godot executable not found: %GODOT_EXE%
    pause
    exit /b 1
)

start "" "%GODOT_EXE%" --path "%~dp0"
