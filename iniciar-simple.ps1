# MEDICSYS - Script Simple de Inicio

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   MEDICSYS - Iniciando Sistema" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# Cargar variables de entorno desde .env si existe
$envFile = Join-Path $PSScriptRoot ".env"
if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        $line = $_.Trim()
        if ($line -eq "" -or $line.StartsWith("#")) { return }
        $parts = $line -split "=", 2
        if ($parts.Length -eq 2) {
            [Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim(), "Process")
        }
    }
    Write-Host "Variables de entorno cargadas desde .env" -ForegroundColor Green
}

# 1. Iniciar Backend en una nueva ventana
Write-Host "`n1. Iniciando Backend..." -ForegroundColor Yellow
$backendPath = "C:\MEDICSYS\MEDICSYS\MEDICSYS.Api"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$backendPath'; dotnet run"
Write-Host "   Backend iniciando..." -ForegroundColor Green

# 2. Esperar un momento
Start-Sleep -Seconds 3

# 3. Iniciar Frontend en una nueva ventana  
Write-Host "`n2. Iniciando Frontend..." -ForegroundColor Yellow
$frontendPath = "C:\MEDICSYS\MEDICSYS\MEDICSYS.Web"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$frontendPath'; npm start"
Write-Host "   Frontend iniciando..." -ForegroundColor Green

Write-Host "`n3. Esperando que los servicios esten listos..." -ForegroundColor Yellow
Write-Host "   (Esto puede tardar 1-2 minutos)" -ForegroundColor Gray

Start-Sleep -Seconds 15

Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "   SERVICIOS INICIADOS" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "URLs:" -ForegroundColor White
Write-Host "  Backend:  http://localhost:5154" -ForegroundColor Cyan
Write-Host "  Frontend: http://localhost:4200" -ForegroundColor Cyan
Write-Host ""
Write-Host "Credenciales de Estudiante:" -ForegroundColor Yellow
Write-Host "  Email:    estudiante1@medicsys.com" -ForegroundColor Green
Write-Host "  Password: Estudiante123!" -ForegroundColor Green
Write-Host ""
Write-Host "IMPORTANTE:" -ForegroundColor Yellow
Write-Host "  - Los servicios estan corriendo en ventanas separadas" -ForegroundColor White
Write-Host "  - NO cierres esas ventanas mientras uses el sistema" -ForegroundColor White
Write-Host ""

# Abrir navegador
Start-Sleep -Seconds 5
Write-Host "Abriendo navegador..." -ForegroundColor Cyan
Start-Process "http://localhost:4200"

Write-Host "`nSistema listo para usar!" -ForegroundColor Green
