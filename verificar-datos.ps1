# Script de Pruebas y Verificación del Sistema MEDICSYS
$ErrorActionPreference = "Continue"
$apiUrl = "http://localhost:5154/api"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "   MEDICSYS - PRUEBAS DE SISTEMA" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Autenticación
Write-Host "[1/6] Autenticando..." -ForegroundColor Yellow
$loginOdon = '{"email":"odontologo1@medicsys.com","password":"Odontologo123!"}'
$loginEst = '{"email":"estudiante1@medicsys.com","password":"Estudiante123!"}'

$resOdon = Invoke-RestMethod -Uri "$apiUrl/auth/login" -Method Post -ContentType "application/json" -Body $loginOdon
$resEst = Invoke-RestMethod -Uri "$apiUrl/auth/login" -Method Post -ContentType "application/json" -Body $loginEst

$headersOdon = @{Authorization="Bearer $($resOdon.token)"}
$headersEst = @{Authorization="Bearer $($resEst.token)"}
Write-Host "   ✅ Login exitoso" -ForegroundColor Green

# Pacientes
Write-Host "`n[2/6] Verificando pacientes..." -ForegroundColor Yellow
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$pacientes = Invoke-RestMethod -Uri "$apiUrl/patients" -Headers $headersOdon
$sw.Stop()
Write-Host "   ✅ $($pacientes.Length) pacientes ($($sw.ElapsedMilliseconds)ms)" -ForegroundColor Green

# Historias
Write-Host "`n[3/6] Verificando historias clínicas..." -ForegroundColor Yellow
$sw.Restart()
$historias = Invoke-RestMethod -Uri "$apiUrl/clinical-histories" -Headers $headersEst
$sw.Stop()
Write-Host "   ✅ $($historias.Length) historias ($($sw.ElapsedMilliseconds)ms)" -ForegroundColor Green

# Facturas
Write-Host "`n[4/6] Verificando facturas..." -ForegroundColor Yellow
$sw.Restart()
$facturas = Invoke-RestMethod -Uri "$apiUrl/invoices" -Headers $headersOdon
$sw.Stop()
$totalFact = ($facturas | ForEach-Object {$_.total} | Measure-Object -Sum).Sum
Write-Host "   ✅ $($facturas.Length) facturas ($($sw.ElapsedMilliseconds)ms)" -ForegroundColor Green
Write-Host "   Total facturado: `$$([math]::Round($totalFact, 2))" -ForegroundColor Cyan

# Contabilidad
Write-Host "`n[5/6] Verificando movimientos contables..." -ForegroundColor Yellow
$sw.Restart()
$entries = Invoke-RestMethod -Uri "$apiUrl/accounting/entries" -Headers $headersOdon
$sw.Stop()
Write-Host "   ✅ $($entries.Length) movimientos ($($sw.ElapsedMilliseconds)ms)" -ForegroundColor Green

if ($entries.Length -gt 0) {
    $ingresos = $entries | Where-Object {$_.category.type -eq "Income"}
    $gastos = $entries | Where-Object {$_.category.type -eq "Expense"}
    $totalIng = ($ingresos | ForEach-Object {$_.amount} | Measure-Object -Sum).Sum
    $totalGas = ($gastos | ForEach-Object {$_.amount} | Measure-Object -Sum).Sum
    $balance = $totalIng - $totalGas
    
    Write-Host "`n   RESUMEN CONTABLE:" -ForegroundColor Cyan
    Write-Host "   - Ingresos: $($ingresos.Length) = `$$([math]::Round($totalIng, 2))" -ForegroundColor Green
    Write-Host "   - Gastos: $($gastos.Length) = `$$([math]::Round($totalGas, 2))" -ForegroundColor Red
    Write-Host "   - Balance: `$$([math]::Round($balance, 2))" -ForegroundColor $(if($balance -gt 0){"Green"}else{"Red"})
}

# Categorías
Write-Host "`n[6/6] Verificando categorías..." -ForegroundColor Yellow
$cats = Invoke-RestMethod -Uri "$apiUrl/accounting/categories" -Headers $headersOdon
Write-Host "   ✅ $($cats.Length) categorías" -ForegroundColor Green

Write-Host "`n================================================" -ForegroundColor Cyan
Write-Host "✅ TODAS LAS PRUEBAS COMPLETADAS" -ForegroundColor Green
Write-Host "================================================`n" -ForegroundColor Cyan
