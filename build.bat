@echo off
setlocal

echo Starting VeSCU build...
echo.

set "ARCH=win-x64"
if /i "%PROCESSOR_ARCHITECTURE%"=="ARM64" set "ARCH=win-arm64"
if /i "%PROCESSOR_ARCHITEW6432%"=="ARM64" set "ARCH=win-arm64"

set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"

if not exist "%ISCC%" (
    echo [ERROR] Inno Setup compiler ^(ISCC.exe^) was not found.
    echo Please install Inno Setup via:
    echo   winget install JRSoftware.InnoSetup
    echo and make sure it's in your PATH.
    exit /b 1
)

echo [1/2] Publishing C# release binary ^(%ARCH%^)...
dotnet publish src\VeSCU\VeSCU.csproj -c Release -r %ARCH% --self-contained false -p:PublishSingleFile=true
if %ERRORLEVEL% neq 0 (
    echo [ERROR] dotnet publish failed!
    exit /b %ERRORLEVEL%
)

echo.
echo [2/2] Compiling Inno Setup installer...
if exist "installer\out" (
    rmdir /s /q "installer\out"
)
"%ISCC%" /DAppArch=%ARCH% /DAppVersion="dev" installer\setup.iss
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Inno Setup failed!
    exit /b %ERRORLEVEL%
)

echo Build successful.

endlocal