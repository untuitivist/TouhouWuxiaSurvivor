@echo off
setlocal
cd /d "%~dp0..\.."
if not exist artifacts mkdir artifacts
set "GODOT_EXE=D:\_soft\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe"
for %%S in (title heroes help settings combat choices pause boss result) do (
    echo Capturing %%S
    "%GODOT_EXE%" --path . --audio-driver Dummy -- --rebirth-screen=%%S --rebirth-capture=res://artifacts/%%S.png > artifacts\capture-%%S.log 2>&1
    if errorlevel 1 exit /b 1
    type artifacts\capture-%%S.log
    findstr /c:"REBIRTH_CAPTURE_PASS" artifacts\capture-%%S.log > nul
    if errorlevel 1 exit /b 1
)
echo REBIRTH_CAPTURE_SUITE_PASS
"%GODOT_EXE%" --path . --resolution 960x540 --audio-driver Dummy -- --rebirth-screen=choices --rebirth-capture=res://artifacts/choices-960.png > artifacts\capture-choices-960.log 2>&1
if errorlevel 1 exit /b 1
type artifacts\capture-choices-960.log
findstr /c:"REBIRTH_CAPTURE_PASS" artifacts\capture-choices-960.log > nul
if errorlevel 1 exit /b 1
exit /b 0
