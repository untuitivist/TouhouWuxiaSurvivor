@echo off
setlocal
cd /d "%~dp0..\.."
set DOTNET_CLI_UI_LANGUAGE=en
if not exist artifacts mkdir artifacts
echo [1/3] Build independent Godot runtime
dotnet build TouhouWuxiaSurvivor.csproj --configuration Debug > artifacts\build.log 2>&1
if errorlevel 1 goto build_failed
type artifacts\build.log
findstr /c:"Build FAILED" artifacts\build.log > nul
if not errorlevel 1 exit /b 1
echo [2/3] Run deterministic core regressions
dotnet run --project tests\rebirth\Rebirth.Tests.csproj --configuration Release -- --balance > artifacts\core-tests.log 2>&1
if errorlevel 1 goto core_failed
type artifacts\core-tests.log
echo [3/3] Godot runtime and UI smoke
set "GODOT_EXE=D:\_soft\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe"
"%GODOT_EXE%" --headless --path . -- --rebirth-smoke > artifacts\ui-smoke.log 2>&1
if errorlevel 1 goto ui_failed
type artifacts\ui-smoke.log
findstr /c:"REBIRTH_UI_SMOKE_PASS" artifacts\ui-smoke.log > nul
if errorlevel 1 exit /b 1
findstr /b /c:"ERROR:" artifacts\ui-smoke.log > nul
if not errorlevel 1 exit /b 1
echo REBIRTH_VALIDATION_PASS
exit /b 0
:build_failed
type artifacts\build.log
exit /b 1
:core_failed
type artifacts\core-tests.log
exit /b 1
:ui_failed
type artifacts\ui-smoke.log
exit /b 1
