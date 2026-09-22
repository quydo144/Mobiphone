@echo off
chcp 65001 >nul
title Mobiphone Build Menu

:menu
cls
echo ================================================
echo Mobiphone Build Script
echo ================================================
echo.
echo Chọn action:
echo.
echo [1] Build MSI installer
echo [2] Gỡ và cài lại ứng dụng
echo [3] Dọn dẹp build files
echo [4] Thoát
echo.
set /p choice="Nhập số (1-4): "

if "%choice%"=="1" goto build
if "%choice%"=="2" goto reinstall
if "%choice%"=="3" goto clean
if "%choice%"=="4" goto end
echo Lựa chọn không hợp lệ!
timeout /t 2 >nul
goto menu

:build
cls
echo ================================================
echo Building Mobiphone Installer
echo ================================================
echo.

echo [1/7] Cleaning old build files...
if exist "bin\Release" rmdir /s /q "bin\Release" 2>nul
if exist "obj" rmdir /s /q "obj" 2>nul
if exist "Installer\obj" rmdir /s /q "Installer\obj" 2>nul
if exist "Installer\HarvestedFiles.wxs" del /f /q "Installer\HarvestedFiles.wxs" 2>nul
if exist "*.msi" del /f /q "*.msi" 2>nul
if exist "*.wixpdb" del /f /q "*.wixpdb" 2>nul
echo Cleanup complete
echo.

echo [2/7] Restoring dependencies...
dotnet restore Mobiphone.sln
if errorlevel 1 (
    echo Restore failed!
    pause
    goto end
)
echo Restore succeeded
echo.

echo [3/7] Building in Release mode...
dotnet build Mobiphone.csproj --no-restore --configuration Release
if errorlevel 1 (
    echo Build failed!
    pause
    goto end
)
echo Build succeeded
echo.

echo [4/7] Publishing with all dependencies...
dotnet publish Mobiphone.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=true -o bin/Release/net8.0-windows/win-x64/publish
if errorlevel 1 (
    echo Publish failed!
    pause
    goto end
)
echo Publish succeeded
echo.

echo [5/7] Checking WiX installation...
where wix >nul 2>&1
if errorlevel 1 (
    echo Installing WiX...
    dotnet tool install --global wix --version 4.0.5
    if errorlevel 1 (
        echo Failed to install WiX!
        pause
        goto end
    )
)
echo WiX ready
echo.

echo [6/7] Generating WiX components...
powershell -ExecutionPolicy Bypass -Command "$publishPath = Resolve-Path 'bin\Release\net8.0-windows\win-x64\publish'; & 'Installer\Generate-Components.ps1' -PublishPath $publishPath -OutputFile 'Installer\HarvestedFiles.wxs'"
if errorlevel 1 (
    echo Failed to generate components!
    pause
    goto end
)
echo.

echo [7/7] Building MSI installer...
for /f "usebackq delims=" %%V in (`powershell -NoProfile -Command "[xml]$p = Get-Content 'Mobiphone.csproj'; $p.Project.PropertyGroup.Version"`) do set "APP_VERSION=%%V"
if not defined APP_VERSION (
    echo Khong doc duoc phien ban tu Mobiphone.csproj!
    pause
    goto end
)
echo Installer version: %APP_VERSION%
cd Installer
wix build Package.wxs HarvestedFiles.wxs -arch x64 -d PublishDir="..\bin\Release\net8.0-windows\win-x64\publish" -d ProductVersion="%APP_VERSION%" -o "..\SC Hoang Quoc.msi"
set BUILD_RESULT=%errorlevel%
cd ..

if %BUILD_RESULT% neq 0 (
    echo MSI build failed!
    pause
    goto end
)
echo.
echo ================================================
echo Build Complete!
echo ================================================
echo.
echo MSI Installer: SC Hoang Quoc.msi
for %%F in (SC Hoang Quoc.msi) do echo Size: %%~zF bytes
echo.
pause
goto menu

:reinstall
cls
echo ================================================
echo Reinstall Mobiphone
echo ================================================
echo.

if not exist "SC Hoang Quoc.msi" (
    echo Không tìm thấy SC Hoang Quoc.msi!
    echo Chạy Build MSI trước.
    pause
    goto menu
)

echo [1/3] Kiểm tra phiên bản cũ...
wmic product where "name='SC Hoang Quoc'" get name,version 2>nul | findstr /i "SC Hoang Quoc" >nul
if not errorlevel 1 (
    echo Đang gỡ phiên bản cũ...
    wmic product where "name='SC Hoang Quoc'" call uninstall /nointeractive >nul 2>&1
    echo Đã gỡ phiên bản cũ
    timeout /t 2 >nul
) else (
    echo Không tìm thấy phiên bản cũ
)
echo.

echo [2/3] Dọn dẹp registry...
reg delete "HKCU\Software\SC Hoang Quoc" /f >nul 2>&1
echo Registry cleaned
echo.

echo [3/3] Cài đặt phiên bản mới...
msiexec /i "%CD%\SC Hoang Quoc.msi" /qb
echo Cài đặt hoàn tất!
echo.

echo ================================================
echo Kiểm Tra Cài Đặt
echo ================================================
echo.

if exist "C:\Program Files\SC Hoang Quoc\SC Hoang Quoc.exe" (
    echo [OK] Executable
) else (
    echo [FAIL] Executable
)

if exist "C:\Program Files\SC Hoang Quoc\SQLite.Interop.dll" (
    echo [OK] SQLite DLL
) else (
    echo [FAIL] SQLite DLL
)

if exist "%USERPROFILE%\Desktop\SC Hoang Quoc.lnk" (
    echo [OK] Desktop Shortcut
) else (
    echo [FAIL] Desktop Shortcut
)

echo.
echo Database location:
echo %LOCALAPPDATA%\SC Hoang Quoc\records.db
echo.

set /p launch="Mở ứng dụng ngay? (Y/N): "
if /i "%launch%"=="Y" (
    start "" "C:\Program Files\SC Hoang Quoc\SC Hoang Quoc.exe"
    timeout /t 3 >nul
    if exist "%LOCALAPPDATA%\SC Hoang Quoc\records.db" (
        echo Database đã được tạo!
    )
)
echo.
pause
goto menu

:clean
cls
echo ================================================
echo Cleanup Build Files
echo ================================================
echo.

echo Cleaning...
if exist "bin" (
    rmdir /s /q "bin" 2>nul
    echo   Removed: bin
)
if exist "obj" (
    rmdir /s /q "obj" 2>nul
    echo   Removed: obj
)
if exist "Installer\obj" (
    rmdir /s /q "Installer\obj" 2>nul
    echo   Removed: Installer\obj
)
if exist "Installer\HarvestedFiles.wxs" (
    del /f /q "Installer\HarvestedFiles.wxs" 2>nul
    echo   Removed: Installer\HarvestedFiles.wxs
)
if exist "*.msi" (
    del /f /q "*.msi" 2>nul
    echo   Removed: *.msi
)
if exist "*.wixpdb" (
    del /f /q "*.wixpdb" 2>nul
    echo   Removed: *.wixpdb
)
if exist ".wix" (
    rmdir /s /q ".wix" 2>nul
    echo   Removed: .wix
)

echo.
echo Cleanup complete!
echo.
pause
goto menu

:end
exit
