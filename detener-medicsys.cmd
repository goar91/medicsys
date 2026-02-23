@echo off
setlocal
cd /d "%~dp0"

echo ================================================
echo    MEDICSYS - Detener Servicios (Windows)
echo ================================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\stop-medicsys-windows.ps1"
if errorlevel 1 (
  echo.
  echo Error al detener MEDICSYS.
  pause
  exit /b 1
)

echo.
echo Detencion completada.
endlocal
