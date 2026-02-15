@echo off
REM MEDICSYS - Script de Inicialización usando CMD
REM Ejecuta como administrador

cd /d "%~dp0"

echo ================================================
echo    MEDICSYS - Inicio del Sistema
echo ================================================
echo.

echo Ejecutando script de PowerShell...
powershell.exe -ExecutionPolicy Bypass -File "%~dp0iniciar-medicsys.ps1"

pause
