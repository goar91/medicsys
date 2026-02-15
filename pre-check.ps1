# Pre-Check - Verificar que todo esta listo antes de ejecutar

Write-Host @"
================================================
   MEDICSYS - PRE-CHECK
   Verificando prerequisitos...
================================================
"@ -ForegroundColor Cyan

$allOk = $true

# 1. Verificar PowerShell
Write-Host "`n[1/6] PowerShell..." -ForegroundColor Yellow
if ($PSVersionTable.PSVersion.Major -ge 5) {
    Write-Host "   OK - PowerShell $($PSVersionTable.PSVersion)" -ForegroundColor Green
} else {
    Write-Host "   ERROR - Se requiere PowerShell 5+" -ForegroundColor Red
    $allOk = $false
}

# 2. Verificar archivos de scripts
Write-Host "`n[2/6] Scripts..." -ForegroundColor Yellow
$scripts = @("datos-4-meses.ps1", "verificar-datos.ps1", "balance-contable.ps1", "ejecutar-todo.ps1")
foreach ($script in $scripts) {
    if (Test-Path $script) {
        Write-Host "   OK - $script existe" -ForegroundColor Green
    } else {
        Write-Host "   ERROR - $script no encontrado" -ForegroundColor Red
        $allOk = $false
    }
}

# 3. Verificar directorio de backend
Write-Host "`n[3/6] Backend..." -ForegroundColor Yellow
if (Test-Path "MEDICSYS.Api\MEDICSYS.Api.csproj") {
    Write-Host "   OK - Proyecto backend encontrado" -ForegroundColor Green
} else {
    Write-Host "   ERROR - Backend no encontrado" -ForegroundColor Red
    $allOk = $false
}

# 4. Verificar directorio de frontend
Write-Host "`n[4/6] Frontend..." -ForegroundColor Yellow
if (Test-Path "MEDICSYS.Web\package.json") {
    Write-Host "   OK - Proyecto frontend encontrado" -ForegroundColor Green
} else {
    Write-Host "   ADVERTENCIA - Frontend no encontrado" -ForegroundColor Yellow
}

# 5. Verificar si backend esta corriendo
Write-Host "`n[5/6] Backend Status..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5154" -TimeoutSec 2 -ErrorAction Stop
    Write-Host "   OK - Backend esta corriendo" -ForegroundColor Green
} catch {
    Write-Host "   ADVERTENCIA - Backend no esta corriendo" -ForegroundColor Yellow
    Write-Host "   Iniciar con: cd MEDICSYS.Api; dotnet run" -ForegroundColor Gray
}

# 6. Verificar conectividad de red
Write-Host "`n[6/6] Red..." -ForegroundColor Yellow
try {
    Test-Connection -ComputerName localhost -Count 1 -Quiet | Out-Null
    Write-Host "   OK - Red local funcionando" -ForegroundColor Green
} catch {
    Write-Host "   ERROR - Problema de red" -ForegroundColor Red
    $allOk = $false
}

# Resumen
Write-Host @"

================================================
   RESUMEN
================================================
"@ -ForegroundColor Cyan

if ($allOk) {
    Write-Host @"
Todo esta listo! Puedes ejecutar:

    .\ejecutar-todo.ps1

O paso a paso:
    1. cd MEDICSYS.Api; dotnet run   (si aun no esta corriendo)
    2. .\datos-4-meses.ps1
    3. .\balance-contable.ps1

"@ -ForegroundColor Green
} else {
    Write-Host @"
Hay problemas que debes resolver antes de continuar.
Revisa los errores marcados arriba.

"@ -ForegroundColor Red
}

Write-Host "================================================`n" -ForegroundColor Cyan
