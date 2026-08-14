@echo off
rem Run the experimental D3D12 and Mobile path to reproduce backend-specific stalls.
call "%~dp0run_diagnostics_core.cmd" d3d12
exit /b %errorlevel%
