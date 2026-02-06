@echo off
setlocal
set ROOT=%~dp0
set PID_DIR=%ROOT%.medicsys-pids
set ENV_FILE=%ROOT%.env
if not exist "%PID_DIR%" mkdir "%PID_DIR%"

echo Verifica que PostgreSQL estÃ© en ejecuciÃ³n en el puerto 5432...

echo Iniciando backend...
powershell -NoProfile -Command "$envFile = '%ROOT%.env'; if (-not (Test-Path $envFile)) { Write-Host 'No se encontrÃ³ .env en la raÃ­z. Crea uno desde .env.example y vuelve a ejecutar.'; exit 1 } ; Get-Content $envFile | ForEach-Object { $line = $_.Trim(); if ($line -eq '' -or $line.StartsWith('#')) { return } ; $parts = $line -split '=', 2; if ($parts.Length -eq 2) { [Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim(), 'Process') } } ; $required = @('ConnectionStrings__DefaultConnection','ConnectionStrings__OdontologiaConnection','ConnectionStrings__AcademicoConnection','Jwt__Key'); $missing = $required | Where-Object { [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($_,'Process')) }; if ($missing.Count -gt 0) { Write-Host ('Faltan variables de entorno: ' + ($missing -join ', ')); exit 1 } ; $p = Start-Process -FilePath 'dotnet' -ArgumentList 'run --project "%ROOT%MEDICSYS.Api"' -WorkingDirectory '%ROOT%' -PassThru; $p.Id | Set-Content -Path '%PID_DIR%\backend.pid'"

echo Iniciando frontend...
powershell -NoProfile -Command "$p=Start-Process -FilePath "$env:ProgramFiles\nodejs\npm.cmd" -ArgumentList 'start' -WorkingDirectory '%ROOT%MEDICSYS.Web' -PassThru; $p.Id | Set-Content -Path '%PID_DIR%\frontend.pid'"

echo Abriendo navegador...
powershell -NoProfile -Command "$p=Start-Process 'http://localhost:4200' -PassThru; $p.Id | Set-Content -Path '%PID_DIR%\browser.pid'"

echo Listo.
endlocal
