@echo off
setlocal EnableExtensions

cd /d "%~dp0"

set "PACKAGE_SCRIPT=%~dp0Package-WangDesk.cmd"
set "SETUP_SCRIPT=%~dp0installer\WangDesk.iss"
set "PACKAGE_DIR=%~dp0artifacts\packages\WangDesk-win-x64"
set "INSTALLER_DIR=%~dp0artifacts\installers"
set "OUTPUT_BASE=WangDesk-Setup"
set "SETUP_ALIAS=setup.exe"
set "MODE=%~1"

if /I "%MODE%"=="--help" goto :help

if not exist "%PACKAGE_SCRIPT%" goto :missing_package_script
if not exist "%SETUP_SCRIPT%" goto :missing_setup_script

call "%PACKAGE_SCRIPT%"
if errorlevel 1 goto :package_failed

call :find_iscc
if not defined ISCC_EXE goto :missing_iscc

for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$xml = [xml](Get-Content 'src/WangDesk.App/WangDesk.App.csproj'); $version = @($xml.Project.PropertyGroup.Version | Where-Object { $_ })[0]; if ($version) { $version } else { '1.0.0' }"`) do set "APP_VERSION=%%I"
if not defined APP_VERSION set "APP_VERSION=1.0.0"

if not exist "%INSTALLER_DIR%" mkdir "%INSTALLER_DIR%"
if exist "%INSTALLER_DIR%\%OUTPUT_BASE%.exe" del /f /q "%INSTALLER_DIR%\%OUTPUT_BASE%.exe"
if exist "%INSTALLER_DIR%\%SETUP_ALIAS%" del /f /q "%INSTALLER_DIR%\%SETUP_ALIAS%"

echo Building WangDesk installer...
"%ISCC_EXE%" /Qp "/DMyAppVersion=%APP_VERSION%" "/DMySourceDir=%PACKAGE_DIR%" "/DMyOutputDir=%INSTALLER_DIR%" "/DMyOutputBaseFilename=%OUTPUT_BASE%" "%SETUP_SCRIPT%"
if errorlevel 1 goto :setup_failed

copy /y "%INSTALLER_DIR%\%OUTPUT_BASE%.exe" "%INSTALLER_DIR%\%SETUP_ALIAS%" >nul

echo.
echo Setup ready.
echo Installer: %INSTALLER_DIR%\%OUTPUT_BASE%.exe
echo Alias:     %INSTALLER_DIR%\%SETUP_ALIAS%
echo Share the setup executable with end users, then have them double-click it to install WangDesk.
exit /b 0

:find_iscc
set "ISCC_EXE="
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC_EXE=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not defined ISCC_EXE if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC_EXE=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not defined ISCC_EXE if exist "%LocalAppData%\Programs\Inno Setup 6\ISCC.exe" set "ISCC_EXE=%LocalAppData%\Programs\Inno Setup 6\ISCC.exe"
if not defined ISCC_EXE for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$cmd = Get-Command iscc -ErrorAction SilentlyContinue; if ($cmd) { $cmd.Source }"`) do set "ISCC_EXE=%%I"
exit /b 0

:missing_package_script
echo Package-WangDesk.cmd was not found.
pause
exit /b 1

:missing_setup_script
echo installer\WangDesk.iss was not found.
pause
exit /b 1

:package_failed
echo WangDesk package creation failed, so setup.exe was not built.
pause
exit /b 1

:missing_iscc
echo Inno Setup 6 was not found.
echo Install Inno Setup 6, then run this file again.
echo Suggested command:
echo   winget install --id JRSoftware.InnoSetup --exact --accept-package-agreements --accept-source-agreements
pause
exit /b 1

:setup_failed
echo WangDesk setup build failed.
echo Review the Inno Setup compiler output for details.
pause
exit /b 1

:help
echo Double-click this file to build a Windows setup installer.
echo Output:
echo   artifacts\installers\WangDesk-Setup.exe
echo   artifacts\installers\setup.exe
exit /b 0
