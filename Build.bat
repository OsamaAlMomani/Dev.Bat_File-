@echo off
REM ============================================================
REM Builds AI_Setup_UI.exe as a single self-contained file.
REM Needs the .NET 8 SDK (or newer):  winget install Microsoft.DotNet.SDK.8
REM Output goes to the "dist" folder next to this file.
REM ============================================================

set "HERE=%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERROR] The .NET SDK was not found.
    echo Install it with:  winget install Microsoft.DotNet.SDK.8
    pause
    exit /b 1
)

echo Building AI_Setup_UI.exe ...
dotnet publish "%HERE%AI_Setup_UI.csproj" -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true -o "%HERE%dist"

if errorlevel 1 (
    echo.
    echo [ERROR] Build failed. See the messages above.
    pause
    exit /b 1
)

echo.
echo Done. Your app is here:  %HERE%dist\AI_Setup_UI.exe
echo (The .venv, projects\ folder and setup_log.txt get created next to the exe.)
pause
