@echo off
setlocal
cd /d "%~dp0"
if not exist artifacts\deployment mkdir artifacts\deployment
echo Deployment log: artifacts\deployment\deploy.log
C:\Users\untuitivist\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe -NoProfile -File tools\platform\deploy_web.ps1 %* > artifacts\deployment\deploy.log 2>&1
set "RESULT=%ERRORLEVEL%"
type artifacts\deployment\deploy.log
exit /b %RESULT%
