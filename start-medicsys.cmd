@echo off
setlocal
set ROOT=%~dp0
set PID_DIR=%ROOT%.medicsys-pids
if not exist "%PID_DIR%" mkdir "%PID_DIR%"

echo Verifica que PostgreSQL estÃ© en ejecuciÃ³n en el puerto 5432...

echo Iniciando backend...
powershell -NoProfile -Command "$p=Start-Process -FilePath 'dotnet' -ArgumentList 'run --project "%ROOT%MEDICSYS.Api"' -WorkingDirectory '%ROOT%' -PassThru; $p.Id | Set-Content -Path '%PID_DIR%\backend.pid'"

echo Iniciando frontend...
powershell -NoProfile -Command "$p=Start-Process -FilePath "$env:ProgramFiles\nodejs\npm.cmd" -ArgumentList 'start' -WorkingDirectory '%ROOT%MEDICSYS.Web' -PassThru; $p.Id | Set-Content -Path '%PID_DIR%\frontend.pid'"

echo Abriendo navegador...
powershell -NoProfile -Command "$p=Start-Process 'http://localhost:4200' -PassThru; $p.Id | Set-Content -Path '%PID_DIR%\browser.pid'"

echo Listo.
endlocal
