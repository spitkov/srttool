@echo off
echo Building SrtTool...

REM Clean previous builds
rmdir /s /q "bin\Release" 2>nul
rmdir /s /q "installer" 2>nul

REM Publish the application
dotnet publish -c Release -r win-x64 --self-contained true

REM Create installer directory
mkdir installer 2>nul

REM Check if Inno Setup is installed
if exist "%PROGRAMFILES(X86)%\Inno Setup 6\ISCC.exe" (
    echo Creating installer...
    "%PROGRAMFILES(X86)%\Inno Setup 6\ISCC.exe" installer.iss
    echo Installer created successfully!
    echo You can find it in the 'installer' directory.
) else (
    echo Error: Inno Setup 6 not found!
    echo Please download and install Inno Setup 6 from:
    echo https://jrsoftware.org/isdl.php
)

pause 