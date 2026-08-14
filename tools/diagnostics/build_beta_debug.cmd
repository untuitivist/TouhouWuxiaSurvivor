@echo off
rem Compatibility alias retained for existing local workflows.
call "%~dp0build_diagnostics.cmd" %*
exit /b %errorlevel%
