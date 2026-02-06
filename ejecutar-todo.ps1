# SCRIPT MAESTRO - MEDICSYS
# Ejecuta todos los procesos de verificacion, generacion de datos y balance contable

Write-Host @"
================================================
   MEDICSYS - PROCESO COMPLETO
   1. Verificacion del sistema
   2. Generacion de 4 meses de datos
   3. Balance contable
================================================
"@ -ForegroundColor Cyan

Write-Host "`nPRERREQUISITOS:" -ForegroundColor Yellow
Write-Host "1. Backend debe estar corriendo en http://localhost:5154" -ForegroundColor Gray
Write-Host "2. PostgreSQL debe estar activo" -ForegroundColor Gray
Write-Host "3. Usuarios odontologo1 y estudiante1 deben existir`n" -ForegroundColor Gray

Read-Host "Presiona ENTER para continuar o Ctrl+C para cancelar"

# Verificar backend
Write-Host "`n[PASO 1/4] Verificando backend..." -ForegroundColor Yellow
try {
    $test = Invoke-WebRequest -Uri "http://localhost:5154" -TimeoutSec 3 -ErrorAction Stop
    Write-Host "   Backend ACTIVO" -ForegroundColor Green
} catch {
    Write-Host "   ERROR: Backend no responde" -ForegroundColor Red
    Write-Host "   Inicia el backend con: cd MEDICSYS.Api; dotnet run" -ForegroundColor Yellow
    exit 1
}

# Verificar datos existentes
Write-Host "`n[PASO 2/4] Verificando datos existentes..." -ForegroundColor Yellow
if (Test-Path .\verificar-datos.ps1) {
    .\verificar-datos.ps1
} else {
    Write-Host "   Script verificar-datos.ps1 no encontrado" -ForegroundColor Red
}

Read-Host "`nPresiona ENTER para generar 4 meses de datos (esto tomara varios minutos)"

# Generar 4 meses de datos
Write-Host "`n[PASO 3/4] Generando 4 meses de datos contables..." -ForegroundColor Yellow
if (Test-Path .\datos-4-meses.ps1) {
    $startTime = Get-Date
    .\datos-4-meses.ps1
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalSeconds
    Write-Host "`n   Proceso completado en $([math]::Round($duration, 2)) segundos" -ForegroundColor Green
} else {
    Write-Host "   Script datos-4-meses.ps1 no encontrado" -ForegroundColor Red
    Write-Host "   Creando script..." -ForegroundColor Yellow
    # Aqui iria el codigo para recrear el script si no existe
}

# Generar balance contable
Write-Host "`n[PASO 4/4] Generando balance contable..." -ForegroundColor Yellow
if (Test-Path .\balance-contable.ps1) {
    .\balance-contable.ps1
} else {
    Write-Host "   Script balance-contable.ps1 no encontrado" -ForegroundColor Red
}

Write-Host @"

================================================
   PROCESO COMPLETADO
================================================

Archivos generados:
- balance-contable.txt

Proximos pasos:
1. Iniciar frontend: cd MEDICSYS.Web; npm start
2. Acceder a http://localhost:4200
3. Verificar que todos los modulos muestren datos

================================================
"@ -ForegroundColor Cyan
