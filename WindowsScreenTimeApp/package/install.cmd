@echo off
setlocal
set "APP_NAME=Windows Screen Time"
set "SOURCE=%~dp0"
set "INSTALL_DIR=%LOCALAPPDATA%\Programs\WindowsScreenTime"
set "START_MENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs"
set "DESKTOP=%USERPROFILE%\Desktop"

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
copy /Y "%SOURCE%WindowsScreenTime.exe" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE%app.ico" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE%app-icon.png" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE%README.md" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE%uninstall.cmd" "%INSTALL_DIR%\" >nul

powershell -NoProfile -ExecutionPolicy Bypass -Command "$shell=New-Object -ComObject WScript.Shell; $s=$shell.CreateShortcut('%START_MENU%\Windows Screen Time.lnk'); $s.TargetPath='%INSTALL_DIR%\WindowsScreenTime.exe'; $s.WorkingDirectory='%INSTALL_DIR%'; $s.IconLocation='%INSTALL_DIR%\app.ico'; $s.Save(); $d=$shell.CreateShortcut('%DESKTOP%\Windows Screen Time.lnk'); $d.TargetPath='%INSTALL_DIR%\WindowsScreenTime.exe'; $d.WorkingDirectory='%INSTALL_DIR%'; $d.IconLocation='%INSTALL_DIR%\app.ico'; $d.Save()"

echo %APP_NAME% installed.
echo Install location: %INSTALL_DIR%
start "" "%INSTALL_DIR%\WindowsScreenTime.exe"
endlocal
