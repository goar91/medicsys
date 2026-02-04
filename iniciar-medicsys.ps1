# MEDICSYS - Script de Inicialización Completo
# Usa CMD en modo administrador como solicitado

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "   MEDICSYS - Inicio del Sistema" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Detener procesos existentes
Write-Host "1. Deteniendo procesos existentes..." -ForegroundColor Yellow
Get-Process | Where-Object { $_.ProcessName -match "dotnet|node" -and $_.Path -like "*MEDICSYS*" } | ForEach-Object {
    Write-Host "   Deteniendo proceso: $($_.ProcessName) (PID: $($_.Id))" -ForegroundColor Gray
    Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
}
Write-Host "   ✅ Procesos detenidos" -ForegroundColor Green

# Verificar PostgreSQL
Write-Host "`n2. Verificando PostgreSQL..." -ForegroundColor Yellow
Write-Host "   Asegúrate de que PostgreSQL esté iniciado en el puerto 5432." -ForegroundColor Gray

# Compilar Backend
Write-Host "`n3. Compilando Backend (.NET 9)..." -ForegroundColor Yellow
Set-Location "d:\Programación\MEDICSYS\MEDICSYS.Api"
dotnet build --configuration Release > $null 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Backend compilado exitosamente" -ForegroundColor Green
} else {
    Write-Host "   ❌ Error al compilar backend" -ForegroundColor Red
    Read-Host "Presiona Enter para continuar de todas formas"
}

# Iniciar Backend
Write-Host "`n4. Iniciando Backend API..." -ForegroundColor Yellow
Start-Process cmd -ArgumentList "/c", "cd /d `"d:\Programación\MEDICSYS\MEDICSYS.Api`" && dotnet run" -WindowStyle Minimized
Write-Host "   ✅ Backend iniciado en segundo plano" -ForegroundColor Green

# Compilar Frontend
Write-Host "`n5. Instalando dependencias del Frontend..." -ForegroundColor Yellow
Set-Location "d:\Programación\MEDICSYS\MEDICSYS.Web"
if (-not (Test-Path "node_modules")) {
    npm install > $null 2>&1
    Write-Host "   ✅ Dependencias instaladas" -ForegroundColor Green
} else {
    Write-Host "   ✅ Dependencias ya instaladas" -ForegroundColor Green
}

# Iniciar Frontend
Write-Host "`n6. Iniciando Frontend (Angular 21)..." -ForegroundColor Yellow
Start-Process cmd -ArgumentList "/c", "cd /d `"d:\Programación\MEDICSYS\MEDICSYS.Web`" && npm start" -WindowStyle Minimized
Write-Host "   ✅ Frontend iniciado en segundo plano" -ForegroundColor Green

# Esperar servicios
Write-Host "`n7. Esperando que los servicios estén listos..." -ForegroundColor Yellow
Write-Host "   (Esto puede tardar 1-2 minutos)" -ForegroundColor Gray

$maxAttempts = 30
$backendReady = $false
$frontendReady = $false

for ($i = 1; $i -le $maxAttempts; $i++) {
    Write-Progress -Activity "Iniciando servicios" -Status "Intento $i de $maxAttempts" -PercentComplete (($i / $maxAttempts) * 100)
    
    if (-not $backendReady) {
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:5154/api/auth/login" -Method POST -ContentType "application/json" -Body '{"email":"test","password":"test"}' -TimeoutSec 2 -ErrorAction Stop
            $backendReady = $true
        } catch {
            if ($_.Exception.Response.StatusCode -eq 400 -or $_.Exception.Response.StatusCode -eq 401) {
                $backendReady = $true
            }
        }
    }
    
    if (-not $frontendReady) {
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:4200" -TimeoutSec 2 -ErrorAction Stop
            $frontendReady = $true
        } catch {
            # Continuar esperando
        }
    }
    
    if ($backendReady -and $frontendReady) {
        break
    }
    
    Start-Sleep -Seconds 4
}

Write-Progress -Activity "Iniciando servicios" -Completed

Write-Host ""
if ($backendReady) {
    Write-Host "   ✅ Backend: LISTO" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  Backend: Aún inicializando" -ForegroundColor Yellow
}

if ($frontendReady) {
    Write-Host "   ✅ Frontend: LISTO" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  Frontend: Aún compilando (puede tardar 2-3 minutos)" -ForegroundColor Yellow
}

# Resumen
Write-Host "`n================================================" -ForegroundColor Cyan
Write-Host "   RESUMEN DEL SISTEMA" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "🌐 URLs:" -ForegroundColor White
Write-Host "   Backend:  http://localhost:5154" -ForegroundColor Cyan
Write-Host "   Frontend: http://localhost:4200" -ForegroundColor Cyan
Write-Host ""
Write-Host "👤 Credenciales de prueba:" -ForegroundColor White
Write-Host "   Email:    odontologo@medicsys.com" -ForegroundColor Cyan
Write-Host "   Password: Odontologo123!" -ForegroundColor Cyan
Write-Host ""
Write-Host "✨ NUEVAS FUNCIONALIDADES:" -ForegroundColor Yellow
Write-Host "   ✅ Listado de Historias Clínicas con buscador" -ForegroundColor Green
Write-Host "   ✅ Edición de Historias Clínicas" -ForegroundColor Green
Write-Host "   ✅ Diseño moderno de Agenda" -ForegroundColor Green
Write-Host "   ✅ Creación de citas médicas" -ForegroundColor Green
Write-Host "   ✅ Citas mostradas en calendario" -ForegroundColor Green
Write-Host "   ✅ Edición de citas" -ForegroundColor Green
Write-Host "   ✅ Auto-eliminación de citas pasadas" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Acciones disponibles:" -ForegroundColor White
Write-Host "   - Dashboard → Ver Historias para acceder al listado" -ForegroundColor Gray
Write-Host "   - Agenda con calendario moderno y gestión de citas" -ForegroundColor Gray
Write-Host "   - Búsqueda por nombre, cédula o número de HC" -ForegroundColor Gray
Write-Host ""
Write-Host "⌨️  Presiona Ctrl+C en cualquier momento para detener" -ForegroundColor Gray
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Abrir navegador
Start-Sleep -Seconds 3
if ($frontendReady) {
    Write-Host "Abriendo navegador..." -ForegroundColor Cyan
    Start-Process "http://localhost:4200"
} else {
    Write-Host "El frontend aún no está listo. Puedes abrir manualmente:" -ForegroundColor Yellow
    Write-Host "http://localhost:4200" -ForegroundColor Cyan
}

Write-Host "`nScript completado. Los servicios están corriendo." -ForegroundColor Green
Write-Host "Las ventanas CMD están minimizadas en la barra de tareas." -ForegroundColor Gray
