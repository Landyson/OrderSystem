@echo off
setlocal
cd /d %~dp0\..
echo Starting OrderSystem...
dotnet --version >nul 2>&1
if errorlevel 1 (
  echo ERROR: dotnet SDK/runtime not found. Use publish script instead.
  exit /b 1
)
cd src\OrderSystem.Web
dotnet restore
dotnet run
