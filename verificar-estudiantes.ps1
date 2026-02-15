# Verificacion Rapida del Sistema de Estudiantes - MEDICSYS
# Este script verifica que todo este configurado correctamente

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "   MEDICSYS - Verificacion de Estudiantes" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar PostgreSQL
Write-Host "1. Verificando PostgreSQL..." -ForegroundColor Yellow
$pgService = Get-Service -Name "postgresql-x64-18" -ErrorAction SilentlyContinue
if ($pgService -and $pgService.Status -eq "Running") {
    Write-Host "   OK - PostgreSQL esta corriendo" -ForegroundColor Green
} else {
    Write-Host "   ERROR - PostgreSQL no esta corriendo" -ForegroundColor Red
    Write-Host "   Por favor, inicia PostgreSQL antes de continuar" -ForegroundColor Yellow
    exit 1
}

# 2. Verificar archivos de configuracion
Write-Host "`n2. Verificando archivos de configuracion..." -ForegroundColor Yellow

$appsettingsPath = "C:\MEDICSYS\MEDICSYS\MEDICSYS.Api\appsettings.json"
if (Test-Path $appsettingsPath) {
    $content = Get-Content $appsettingsPath -Raw
    if ($content -match '"DefaultProfessorEmail":\s*"profesor@medicsys.com"') {
        Write-Host "   OK - appsettings.json configurado correctamente" -ForegroundColor Green
    } else {
        Write-Host "   AVISO - appsettings.json puede tener configuracion antigua" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ERROR - No se encuentra appsettings.json" -ForegroundColor Red
}

# 3. Verificar credenciales documentadas
Write-Host "`n3. Verificando credenciales..." -ForegroundColor Yellow
$credsPath = "C:\MEDICSYS\MEDICSYS\Usuarios y claves.txt"
if (Test-Path $credsPath) {
    $content = Get-Content $credsPath -Raw
    if ($content -match 'estudiante1@medicsys.com') {
        Write-Host "   OK - Credenciales actualizadas" -ForegroundColor Green
    } else {
        Write-Host "   AVISO - Archivo de credenciales puede estar desactualizado" -ForegroundColor Yellow
    }
} else {
    Write-Host "   AVISO - No se encuentra archivo de credenciales" -ForegroundColor Yellow
}

# 4. Mostrar credenciales de prueba
Write-Host "`n4. Credenciales para Pruebas:" -ForegroundColor Cyan
Write-Host ""
Write-Host "   ESTUDIANTES:" -ForegroundColor White
Write-Host "   Email:    estudiante1@medicsys.com" -ForegroundColor Green
Write-Host "   Password: Estudiante123!" -ForegroundColor Green
Write-Host ""
Write-Host "   PROFESOR:" -ForegroundColor White
Write-Host "   Email:    profesor@medicsys.com" -ForegroundColor Cyan
Write-Host "   Password: Profesor123!" -ForegroundColor Cyan
Write-Host ""
Write-Host "   ODONTOLOGO:" -ForegroundColor White
Write-Host "   Email:    odontologo@medicsys.com" -ForegroundColor Magenta
Write-Host "   Password: Odontologo123!" -ForegroundColor Magenta
Write-Host ""

# 5. Verificar si el backend esta corriendo
Write-Host "5. Verificando servicios..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5154/api/auth/login" -Method POST -ContentType "application/json" -Body '{"email":"test","password":"test"}' -TimeoutSec 2 -ErrorAction Stop
    Write-Host "   OK - Backend esta corriendo en http://localhost:5154" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 400 -or $_.Exception.Response.StatusCode -eq 401) {
        Write-Host "   OK - Backend esta corriendo en http://localhost:5154" -ForegroundColor Green
    } else {
        Write-Host "   AVISO - Backend no esta corriendo" -ForegroundColor Yellow
        Write-Host "      Ejecuta: .\iniciar-medicsys.ps1" -ForegroundColor Gray
    }
}

try {
    $response = Invoke-WebRequest -Uri "http://localhost:4200" -TimeoutSec 2 -ErrorAction Stop
    Write-Host "   OK - Frontend esta corriendo en http://localhost:4200" -ForegroundColor Green
} catch {
    Write-Host "   AVISO - Frontend no esta corriendo" -ForegroundColor Yellow
    Write-Host "      Ejecuta: .\iniciar-medicsys.ps1" -ForegroundColor Gray
}

# 6. Instrucciones
Write-Host "`n6. Pasos para probar:" -ForegroundColor Cyan
Write-Host "   1. Si los servicios no estan corriendo, ejecuta: .\iniciar-medicsys.ps1" -ForegroundColor White
Write-Host "   2. Abre http://localhost:4200 en el navegador" -ForegroundColor White
Write-Host "   3. Inicia sesion como estudiante con: estudiante1@medicsys.com / Estudiante123!" -ForegroundColor White
Write-Host "   4. Deberias ver el Dashboard de Estudiante" -ForegroundColor White
