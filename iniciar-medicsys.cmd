@echo off
setlocal
cd /d "%~dp0"

call "%~dp0start-medicsys.cmd"
set EXIT_CODE=%ERRORLEVEL%

if not "%EXIT_CODE%"=="0" (
  echo.
  echo El inicio fallo con codigo %EXIT_CODE%.
)

pause
endlocal & exit /b %EXIT_CODE%
