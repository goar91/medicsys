@echo off
setlocal
set ROOT=%~dp0
set PID_DIR=%ROOT%.medicsys-pids

echo Deteniendo frontend y backend...
if exist "%PID_DIR%\frontend.pid" (
  for /f %%p in (%PID_DIR%\frontend.pid) do powershell -NoProfile -Command "Stop-Process -Id %%p -Force -ErrorAction SilentlyContinue"
)
if exist "%PID_DIR%\backend.pid" (
  for /f %%p in (%PID_DIR%\backend.pid) do powershell -NoProfile -Command "Stop-Process -Id %%p -Force -ErrorAction SilentlyContinue"
)
if exist "%PID_DIR%\browser.pid" (
  for /f %%p in (%PID_DIR%\browser.pid) do powershell -NoProfile -Command "Stop-Process -Id %%p -Force -ErrorAction SilentlyContinue"
)

echo Deteniendo PostgreSQL...
docker compose -f "%ROOT%docker-compose.yml" down

if exist "%PID_DIR%" (
  del /q "%PID_DIR%\*.pid" >nul 2>&1
)

echo Listo.
endlocal
