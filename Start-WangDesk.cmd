@echo off
setlocal EnableExtensions

cd /d "%~dp0"

set "PROJECT=src\WangDesk.App\WangDesk.App.csproj"
set "RELEASE_EXE=src\WangDesk.App\bin\Release\net9.0-windows\WangDesk.App.exe"
set "DEBUG_EXE=src\WangDesk.App\bin\Debug\net9.0-windows\WangDesk.App.exe"
set "MODE=%~1"

if /I "%MODE%"=="--help" goto :help

call :resolve_app

if /I "%MODE%"=="--check" (
  if defined APP_EXE (
    echo READY:%APP_EXE%
    exit /b 0
  )

  where dotnet >nul 2>nul
  if errorlevel 1 (
    echo MISSING_DOTNET
    exit /b 1
  )

  echo BUILD_REQUIRED
  exit /b 0
)

if not defined APP_EXE (
  echo Preparing WangDesk for first launch...
  where dotnet >nul 2>nul
  if errorlevel 1 goto :missing_dotnet

  dotnet build "%PROJECT%" -c Debug -v minimal
  if errorlevel 1 goto :build_failed

  call :resolve_app
  if not defined APP_EXE goto :build_failed
)

echo Launching WangDesk...
echo If you do not see a window, check the system tray near the clock.
start "" "%APP_EXE%"
exit /b 0

:resolve_app
set "APP_EXE="
if exist "%RELEASE_EXE%" set "APP_EXE=%RELEASE_EXE%"
if not defined APP_EXE if exist "%DEBUG_EXE%" set "APP_EXE=%DEBUG_EXE%"
exit /b 0

:missing_dotnet
echo dotnet 9 SDK was not found.
echo Install .NET 9 SDK, or use a packaged release build that includes WangDesk.App.exe.
pause
exit /b 1

:build_failed
echo WangDesk could not be built automatically.
echo Open WangDesk.sln in Visual Studio for the full build output.
pause
exit /b 1

:help
echo Double-click this file to launch WangDesk.
echo Optional: --check verifies whether WangDesk is ready without starting it.
exit /b 0
