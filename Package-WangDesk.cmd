@echo off
setlocal EnableExtensions

cd /d "%~dp0"

set "PROJECT=src\WangDesk.App\WangDesk.App.csproj"
set "RUNTIME=win-x64"
set "PACKAGE_ROOT=artifacts\packages"
set "PUBLISH_DIR=%PACKAGE_ROOT%\WangDesk-%RUNTIME%"
set "ZIP_PATH=%PACKAGE_ROOT%\WangDesk-%RUNTIME%.zip"
set "PACKAGE_NOTE=docs\package-readme.txt"
set "MODE=%~1"

if /I "%MODE%"=="--help" goto :help

where dotnet >nul 2>nul
if errorlevel 1 goto :missing_dotnet

if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"
if exist "%ZIP_PATH%" del /f /q "%ZIP_PATH%"
if not exist "%PACKAGE_ROOT%" mkdir "%PACKAGE_ROOT%"

echo Publishing WangDesk for %RUNTIME%...
dotnet publish "%PROJECT%" -c Release -r %RUNTIME% --self-contained true -p:DebugSymbols=false -p:DebugType=None -o "%PUBLISH_DIR%"
if errorlevel 1 goto :publish_failed

if exist "%PACKAGE_NOTE%" copy /y "%PACKAGE_NOTE%" "%PUBLISH_DIR%\README.txt" >nul

echo Creating distributable zip...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%PUBLISH_DIR%\\*' -DestinationPath '%ZIP_PATH%' -Force"
if errorlevel 1 goto :zip_failed

echo.
echo Package ready.
echo Folder: %PUBLISH_DIR%
echo Zip:    %ZIP_PATH%
echo Share the zip with end users, then have them extract it and double-click WangDesk.App.exe.
exit /b 0

:missing_dotnet
echo dotnet 9 SDK was not found.
echo Install the .NET 9 SDK, then run this file again.
pause
exit /b 1

:publish_failed
echo WangDesk publish failed.
echo Open WangDesk.sln in Visual Studio if you need full diagnostics.
pause
exit /b 1

:zip_failed
echo WangDesk was published, but zip creation failed.
echo You can still distribute the folder: %PUBLISH_DIR%
pause
exit /b 1

:help
echo Double-click this file to build a distributable WangDesk package.
echo Output:
echo   artifacts\packages\WangDesk-win-x64\
echo   artifacts\packages\WangDesk-win-x64.zip
exit /b 0
