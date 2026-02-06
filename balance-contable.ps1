# Script de Balance Contable - MEDICSYS
# Genera reporte de balance para 4 meses

$ErrorActionPreference = "Continue"
$apiUrl = "http://localhost:5154/api"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "   MEDICSYS - BALANCE CONTABLE 4 MESES" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Autenticación
Write-Host "Autenticando..." -ForegroundColor Yellow
$login = Invoke-RestMethod -Uri "$apiUrl/auth/login" -Method Post -ContentType "application/json" -Body '{"email":"odontologo1@medicsys.com","password":"Odontologo123!"}'
$headers = @{Authorization="Bearer $($login.token)"}
Write-Host "OK`n" -ForegroundColor Green

# Obtener movimientos contables
Write-Host "Obteniendo movimientos contables..." -ForegroundColor Yellow
$entries = Invoke-RestMethod -Uri "$apiUrl/accounting/entries" -Headers $headers
Write-Host "Total movimientos: $($entries.Length)`n" -ForegroundColor Green

if ($entries.Length -eq 0) {
    Write-Host "No hay movimientos contables. Ejecuta primero datos-4-meses.ps1" -ForegroundColor Red
    exit
}

# Agrupar por mes
Write-Host "BALANCE MENSUAL:" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

$balance = $entries | Group-Object {([datetime]$_.date).ToString("yyyy-MM")} | ForEach-Object {
    $mes = $_.Name
    $mesNombre = switch ($mes) {
        "2025-10" { "Octubre 2025" }
        "2025-11" { "Noviembre 2025" }
        "2025-12" { "Diciembre 2025" }
        "2026-01" { "Enero 2026" }
        default { $mes }
    }
    
    $movimientos = $_.Group
    $ingresos = ($movimientos | Where-Object {$_.category.type -eq "Income"})
    $gastos = ($movimientos | Where-Object {$_.category.type -eq "Expense"})
    
    $totalIngresos = ($ingresos | Measure-Object -Property amount -Sum).Sum
    $totalGastos = ($gastos | Measure-Object -Property amount -Sum).Sum
    $balanceMes = $totalIngresos - $totalGastos
    
    Write-Host "$mesNombre" -ForegroundColor White
    Write-Host "  Ingresos:   $($ingresos.Length) movimientos = `$$([math]::Round($totalIngresos, 2))" -ForegroundColor Green
    Write-Host "  Gastos:     $($gastos.Length) movimientos = `$$([math]::Round($totalGastos, 2))" -ForegroundColor Red
    Write-Host "  Balance:    `$$([math]::Round($balanceMes, 2))" -ForegroundColor $(if($balanceMes -gt 0){"Green"}else{"Red"})
    Write-Host ""
    
    [PSCustomObject]@{
        Mes = $mes
        MesNombre = $mesNombre
        NumIngresos = $ingresos.Length
        TotalIngresos = [math]::Round($totalIngresos, 2)
        NumGastos = $gastos.Length
        TotalGastos = [math]::Round($totalGastos, 2)
        Balance = [math]::Round($balanceMes, 2)
    }
} | Sort-Object Mes

# Totales acumulados
$totalIngresos = ($balance | Measure-Object -Property TotalIngresos -Sum).Sum
$totalGastos = ($balance | Measure-Object -Property TotalGastos -Sum).Sum
$balanceFinal = $totalIngresos - $totalGastos

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "   RESUMEN TOTAL (4 MESES)" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Total Ingresos:  `$$totalIngresos" -ForegroundColor Green
Write-Host "Total Gastos:    `$$totalGastos" -ForegroundColor Red
Write-Host "Balance Final:   `$$balanceFinal" -ForegroundColor $(if($balanceFinal -gt 0){"Green"}else{"Red"})
Write-Host ""

# Detalles por categoría
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "   DESGLOSE POR CATEGORIA" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

$ingresos = $entries | Where-Object {$_.category.type -eq "Income"}
Write-Host "INGRESOS:" -ForegroundColor Green
$ingresos | Group-Object {$_.category.name} | ForEach-Object {
    $total = ($_.Group | Measure-Object -Property amount -Sum).Sum
    Write-Host "  $($_.Name): $($_.Count) movimientos = `$$([math]::Round($total, 2))" -ForegroundColor Gray
}

Write-Host "`nGASTOS:" -ForegroundColor Red
$gastos = $entries | Where-Object {$_.category.type -eq "Expense"}
$gastos | Group-Object {$_.category.name} | ForEach-Object {
    $total = ($_.Group | Measure-Object -Property amount -Sum).Sum
    Write-Host "  $($_.Name): $($_.Count) movimientos = `$$([math]::Round($total, 2))" -ForegroundColor Gray
}

Write-Host "`n================================================" -ForegroundColor Cyan
Write-Host "Balance guardado en: balance-contable.txt" -ForegroundColor Yellow
Write-Host "================================================`n" -ForegroundColor Cyan

# Exportar a archivo
$reporte = @"
BALANCE CONTABLE MEDICSYS
Periodo: Octubre 2025 - Enero 2026
Fecha de generacion: $(Get-Date -Format "dd/MM/yyyy HH:mm:ss")

================================================
RESUMEN MENSUAL
================================================

"@

foreach ($mes in $balance) {
    $reporte += @"
$($mes.MesNombre)
  Ingresos:   $($mes.NumIngresos) movimientos = `$$($mes.TotalIngresos)
  Gastos:     $($mes.NumGastos) movimientos = `$$($mes.TotalGastos)
  Balance:    `$$($mes.Balance)

"@
}

$reporte += @"
================================================
RESUMEN TOTAL (4 MESES)
================================================
Total Ingresos:  `$$totalIngresos
Total Gastos:    `$$totalGastos
Balance Final:   `$$balanceFinal

Rentabilidad: $([math]::Round(($balanceFinal / $totalIngresos) * 100, 2))%

================================================
"@

$reporte | Out-File -FilePath ".\balance-contable.txt" -Encoding UTF8
