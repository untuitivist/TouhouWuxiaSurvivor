@echo off
setlocal DisableDelayedExpansion

rem Discover the sole diagnostic artifact so this launcher does not duplicate the project version.
set "PROFILE=%~1"
set "ARTIFACT_COUNT=0"
for %%F in ("%~dp0TouhouWuxiaSurvivor_*_windows-x86_64-debug.exe") do call :select_artifact "%%~fF" "%%~nF"
if not "%ARTIFACT_COUNT%"=="1" (
    echo Expected exactly one diagnostic executable, found %ARTIFACT_COUNT%.
    exit /b 1
)
set "GAME_VERSION=%ARTIFACT_BASE:TouhouWuxiaSurvivor_=%"
set "GAME_VERSION=%GAME_VERSION:_windows-x86_64-debug=%"
set "CONSOLE_EXE=%~dp0%ARTIFACT_BASE%.console.exe"
if not exist "%GAME_EXE%" (
    echo Main executable not found: %GAME_EXE%
    exit /b 1
)
if not exist "%CONSOLE_EXE%" (
    echo Console wrapper not found: %CONSOLE_EXE%
    exit /b 1
)

rem Select one explicit backend so two sessions can be compared without changing game content.
if /i "%PROFILE%"=="d3d12" set "RENDER_ARGS=--display-driver windows --rendering-method mobile --rendering-driver d3d12"
if /i "%PROFILE%"=="opengl" set "RENDER_ARGS=--display-driver windows --rendering-method gl_compatibility --rendering-driver opengl3"
if not defined RENDER_ARGS (
    echo Unknown diagnostic profile: %PROFILE%
    exit /b 1
)

rem Create an ASCII-only session directory and retry on the extremely unlikely random collision.
set "LOG_ROOT=%~dp0logs"
if not exist "%LOG_ROOT%" mkdir "%LOG_ROOT%"
if errorlevel 1 exit /b %errorlevel%
:create_session
set "SESSION_ID=session_%PROFILE%_%RANDOM%_%RANDOM%_%RANDOM%"
set "SESSION_DIR=%LOG_ROOT%\%SESSION_ID%"
if exist "%SESSION_DIR%" goto create_session
mkdir "%SESSION_DIR%"
if errorlevel 1 exit /b %errorlevel%

rem Record reproducible launch facts before starting the game; the session path is never reused.
>"%SESSION_DIR%\session.txt" echo game_version=%GAME_VERSION%
>>"%SESSION_DIR%\session.txt" echo build_variant=windows-x86_64-debug
>>"%SESSION_DIR%\session.txt" echo renderer_profile=%PROFILE%
>>"%SESSION_DIR%\session.txt" echo processor_architecture=%PROCESSOR_ARCHITECTURE%
>>"%SESSION_DIR%\session.txt" echo processor_count=%NUMBER_OF_PROCESSORS%
ver >"%SESSION_DIR%\windows_version.txt"

rem Verbose engine data identifies the selected adapter; FPS is sampled by Godot without per-frame spam.
echo Starting %PROFILE% diagnostic session. Reproduce the slowdown, then exit the game normally.
"%CONSOLE_EXE%" --verbose --print-fps --log-file "%SESSION_DIR%\godot.log" %RENDER_ARGS% -- "--diagnostic-label=%PROFILE%" "--diagnostic-output=%SESSION_DIR%"
set "GAME_EXIT_CODE=%ERRORLEVEL%"
>>"%SESSION_DIR%\session.txt" echo exit_code=%GAME_EXIT_CODE%

echo Diagnostic session saved to: %SESSION_DIR%
start "" explorer.exe "%SESSION_DIR%"
endlocal & exit /b %GAME_EXIT_CODE%

rem Capture one wildcard result outside a parenthesized block so percent expansion stays reliable.
:select_artifact
set /a ARTIFACT_COUNT+=1
set "GAME_EXE=%~1"
set "ARTIFACT_BASE=%~2"
exit /b 0
