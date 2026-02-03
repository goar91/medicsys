@echo off
setlocal EnableDelayedExpansion
set ROOT=%~dp0
set PID_DIR=%ROOT%.medicsys-pids
set COMPOSE_FILE=%ROOT%docker-compose.yml

echo === Deteniendo servicios MEDICSYS ===

REM Detener procesos usando archivos PID
call :stopPidFile "backend" "%PID_DIR%\backend.pid"
call :stopPidFile "frontend" "%PID_DIR%\frontend.pid"
call :stopPidFile "navegador" "%PID_DIR%\browser.pid"

REM Buscar y detener procesos en puertos especificos
echo Buscando procesos residuales en puertos 5154 y 4200...
for %%p in (5154 4200) do (
  for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":%%p.*LISTENING"') do (
    taskkill /PID %%a /F /T >nul 2>&1
  )
)

REM Cerrar navegadores con localhost:4200
echo Cerrando ventanas del navegador relacionadas con MEDICSYS...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-Process -Name chrome,msedge,firefox,brave,opera -ErrorAction SilentlyContinue | Where-Object {$_.MainWindowTitle -match 'localhost:4200'} | Stop-Process -Force -ErrorAction SilentlyContinue"

REM Detener contenedores Docker
echo Deteniendo contenedores Docker...
docker compose -f "%COMPOSE_FILE%" down --remove-orphans 2>nul

REM Limpiar archivos PID
if exist "%PID_DIR%" (
  del /q "%PID_DIR%\*.pid" >nul 2>&1
)

echo.
echo === Servicios MEDICSYS detenidos correctamente ===
endlocal
exit /b 0

:stopPidFile
set NAME=%~1
set FILE=%~2
if exist "%FILE%" (
  set /p PID_VALUE=<"%FILE%"
  if not "!PID_VALUE!"=="" (
    echo Deteniendo !NAME! ^(PID !PID_VALUE!^)...
    taskkill /PID !PID_VALUE! /T /F >nul 2>&1
  )
) else (
  echo No se encontro archivo PID para !NAME!
)
exit /b 0
