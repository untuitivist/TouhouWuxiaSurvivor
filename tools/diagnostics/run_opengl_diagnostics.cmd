@echo off
rem Run the default OpenGL compatibility path to distinguish renderer faults from game-side load.
call "%~dp0run_diagnostics_core.cmd" opengl
exit /b %errorlevel%
