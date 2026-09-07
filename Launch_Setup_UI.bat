@echo off
REM ============================================================
REM Launches the flexible Setup UI (AI_Setup_UI.ps1).
REM Nothing installs automatically - this only opens the picker
REM window. You choose what to install from there.
REM ============================================================

set "SCRIPT_DIR=%~dp0"

where powershell >nul 2>&1
if errorlevel 1 (
    echo [ERROR] PowerShell was not found on this system.
    echo This UI requires PowerShell ^(included with Windows 10/11^).
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%AI_Setup_UI.ps1"

if errorlevel 1 (
    echo.
    echo The setup window closed with an error. Check setup_log.txt
    echo in this folder for details, if it was created.
    pause
)
