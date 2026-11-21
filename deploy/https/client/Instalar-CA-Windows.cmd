@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Instalar-CA-Windows.ps1" %*
echo.
pause
