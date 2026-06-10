@echo off
setlocal
set "INSTALL_DIR=%LOCALAPPDATA%\Programs\WindowsScreenTime"
set "START_MENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs"
set "DESKTOP=%USERPROFILE%\Desktop"

taskkill /IM WindowsScreenTime.exe /F >nul 2>nul
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v WindowsScreenTime /f >nul 2>nul
del "%START_MENU%\Windows Screen Time.lnk" >nul 2>nul
del "%DESKTOP%\Windows Screen Time.lnk" >nul 2>nul
cd /d "%TEMP%"
rmdir /S /Q "%INSTALL_DIR%" >nul 2>nul
echo Windows Screen Time uninstalled.
echo User data is kept in %%LOCALAPPDATA%%\WindowsScreenTime. Delete it manually if you no longer need it.
endlocal
