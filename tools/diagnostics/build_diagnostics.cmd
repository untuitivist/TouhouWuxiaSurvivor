@echo off
setlocal DisableDelayedExpansion

rem Build diagnostics in an isolated directory without replacing formal artifacts.
set "DEFAULT_GODOT_EXE=D:\_soft\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe"
if not defined GODOT_EXE set "GODOT_EXE=%DEFAULT_GODOT_EXE%"
for %%I in ("%~dp0..\..") do set "PROJECT_DIR=%%~fI"
set "OUTPUT_DIR=%PROJECT_DIR%\release\diagnostics"

rem Read the stage-first version so diagnostics never become another game version.
for /f "tokens=2 delims==" %%V in ('findstr /b /c:"config/version=" "%PROJECT_DIR%\project.godot"') do set "GAME_VERSION=%%~V"
if not defined GAME_VERSION (
    echo Project version not found in project.godot.
    exit /b 1
)

set "ARTIFACT_BASE=TouhouWuxiaSurvivor_%GAME_VERSION%_windows-x86_64-debug"
set "OUTPUT_EXE=%OUTPUT_DIR%\%ARTIFACT_BASE%.exe"
if exist "%OUTPUT_DIR%" (
    echo Refusing to overwrite the existing diagnostic package: %OUTPUT_DIR%
    exit /b 2
)
if not exist "%GODOT_EXE%" (
    echo Godot executable not found: %GODOT_EXE%
    exit /b 1
)

mkdir "%OUTPUT_DIR%"
if errorlevel 1 exit /b %errorlevel%
"%GODOT_EXE%" --headless --path "%PROJECT_DIR%" --export-release "Windows Diagnostics" "%OUTPUT_EXE%"
if errorlevel 1 exit /b %errorlevel%

rem Add both renderer launchers and the collection guide only after export succeeds.
copy /b "%~dp0run_d3d12_diagnostics.cmd" "%OUTPUT_DIR%\Run_Diagnostics_D3D12.cmd" >nul
if errorlevel 1 exit /b %errorlevel%
copy /b "%~dp0run_opengl_diagnostics.cmd" "%OUTPUT_DIR%\Run_Diagnostics_OpenGL.cmd" >nul
if errorlevel 1 exit /b %errorlevel%
copy /b "%~dp0run_diagnostics_core.cmd" "%OUTPUT_DIR%\run_diagnostics_core.cmd" >nul
if errorlevel 1 exit /b %errorlevel%
copy /b "%PROJECT_DIR%\docs\diagnostics.md" "%OUTPUT_DIR%\README_diagnostics.md" >nul
if errorlevel 1 exit /b %errorlevel%

echo Diagnostic package created: %OUTPUT_DIR%
endlocal
