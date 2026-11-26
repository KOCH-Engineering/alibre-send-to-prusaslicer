@echo off
rem Double-click to build and install the "KOCH Send to PrusaSlicer" add-on for Alibre Design (KOCH Engineering).
net session >nul 2>&1
if %errorlevel% neq 0 (
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -Command "& '.\install.ps1' *>&1 | Tee-Object -FilePath '.\install.log'"
echo.
pause
