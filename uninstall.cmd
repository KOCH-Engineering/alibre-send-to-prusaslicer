@echo off
rem Double-click to remove the "KOCH Send to PrusaSlicer" add-on from Alibre Design (close Alibre first).
net session >nul 2>&1
if %errorlevel% neq 0 (
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -Command "& '.\uninstall.ps1' *>&1 | Tee-Object -FilePath '.\uninstall.log'"
echo.
pause
