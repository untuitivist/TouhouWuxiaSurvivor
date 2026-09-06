@echo off
setlocal
cd /d "%~dp0..\.."
set "GODOT_EXE=D:\_soft\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe"
if not exist artifacts mkdir artifacts
"%GODOT_EXE%" --path . --audio-driver Dummy -- --rebirth-video-smoke > artifacts\settings-display.log 2>&1
if errorlevel 1 exit /b 1
type artifacts\settings-display.log
findstr /c:"REBIRTH_DISPLAY_PASS" artifacts\settings-display.log > nul
if errorlevel 1 exit /b 1
findstr /b /c:"ERROR:" artifacts\settings-display.log > nul
if not errorlevel 1 exit /b 1
for %%S in (settings settings-video settings-controls settings-confirm) do (
    for %%R in (1280x720 640x360) do (
        echo Capturing %%S at %%R
        "%GODOT_EXE%" --path . --resolution %%R --audio-driver Dummy -- --rebirth-screen=%%S --rebirth-capture=res://artifacts/settings-%%S-%%R.png > artifacts\settings-%%S-%%R.log 2>&1
        if errorlevel 1 exit /b 1
        type artifacts\settings-%%S-%%R.log
        findstr /c:"REBIRTH_CAPTURE_PASS" artifacts\settings-%%S-%%R.log > nul
        if errorlevel 1 exit /b 1
        findstr /b /c:"ERROR:" artifacts\settings-%%S-%%R.log > nul
        if not errorlevel 1 exit /b 1
    )
)
echo REBIRTH_SETTINGS_RENDER_PASS
exit /b 0
