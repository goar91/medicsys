param(
    [int]$ExpensesPerMonth = 20,
    [int]$PurchasesPerMonth = 10
)

$ErrorActionPreference = "Stop"
$apiUrl = "http://localhost:5154/api"
$delayMs = 80
$requestThrottleMs = 220

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Uri,
        [hashtable]$Headers,
        $Body = $null,
        [int]$MaxRetries = 8
    )

    $attempt = 0
    while ($true) {
        try {
            if ($null -eq $Body) {
                $response = Invoke-RestMethod -Uri $Uri -Method $Method -Headers $Headers -TimeoutSec 30
                Start-Sleep -Milliseconds $requestThrottleMs
                return $response
            }

            $json = $Body | ConvertTo-Json -Depth 10
            $response = Invoke-RestMethod -Uri $Uri -Method $Method -Headers $Headers -ContentType "application/json" -Body $json -TimeoutSec 30
            Start-Sleep -Milliseconds $requestThrottleMs
            return $response
        }
        catch {
            $attempt++

            $statusCode = $null
            if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
                $statusCode = [int]$_.Exception.Response.StatusCode
            }

            if ($statusCode -eq 429 -and $attempt -le $MaxRetries) {
                Start-Sleep -Seconds ([Math]::Min(8 + (2 * $attempt), 20))
                continue
            }

            if ($attempt -le $MaxRetries) {
                Start-Sleep -Seconds ([Math]::Min(2 * $attempt, 6))
                continue
            }
            throw
        }
    }
}

function Normalize-ApiArray {
    param($Value)

    if ($null -eq $Value) {
        return @()
    }

    # Evita arreglos anidados (caso comun al retornar arrays desde funciones en PowerShell)
    if ($Value -is [System.Array] -and $Value.Count -eq 1 -and $Value[0] -is [System.Array]) {
        return @($Value[0])
    }

    return @($Value)
}

function Get-RandomDateInMonthUtc {
    param([datetime]$MonthDate)

    $daysInMonth = [DateTime]::DaysInMonth($MonthDate.Year, $MonthDate.Month)
    $day = Get-Random -Min 1 -Max ($daysInMonth + 1)
    $hour = Get-Random -Min 8 -Max 20
    $minute = Get-Random -Min 0 -Max 60

    return (Get-Date -Year $MonthDate.Year -Month $MonthDate.Month -Day $day -Hour $hour -Minute $minute -Second 0).ToUniversalTime()
}

function Map-PaymentMethodToAccounting {
    param([string]$PaymentMethod)

    switch ($PaymentMethod) {
        "Efectivo" { return "Cash" }
        "Tarjeta" { return "Card" }
        "Transferencia" { return "Transfer" }
        default { return $null }
    }
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host " MEDICSYS - CONTABILIDAD COMPLETA 5 MESES" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# 1) Login odontologo
Write-Host "`n[1/6] Login odontologo..." -ForegroundColor Yellow
$login = @{
    email = "odontologo@medicsys.com"
    password = "Odontologo123!"
}
$auth = Invoke-Api -Method Post -Uri "$apiUrl/auth/login" -Body $login
$headers = @{ Authorization = "Bearer $($auth.token)" }
Write-Host "   OK - autenticado" -ForegroundColor Green

# 2) Inventario base para compras
Write-Host "`n[2/6] Verificando inventario base..." -ForegroundColor Yellow
$inventoryItems = @()
try {
    $inventoryItems = Normalize-ApiArray (Invoke-Api -Method Get -Uri "$apiUrl/odontologia/inventory" -Headers $headers)
}
catch {
    $inventoryItems = @()
}

if ($inventoryItems.Count -lt 30) {
    $toCreate = 30 - $inventoryItems.Count
    Write-Host "   Inventario insuficiente, creando $toCreate artículos..." -ForegroundColor Yellow
    for ($i = 1; $i -le $toCreate; $i++) {
        $itemPayload = @{
            name = "Base-Contab-$([string]::Format('{0:000}', $i))"
            description = "Artículo base para compras y reportes"
            sku = "CNTB-$([string]::Format('{0:0000}', $i))-$((Get-Random -Min 100 -Max 999))"
            quantity = Get-Random -Min 40 -Max 120
            minimumQuantity = Get-Random -Min 8 -Max 20
            unitPrice = [Math]::Round((Get-Random -Min 4 -Max 60) + ((Get-Random -Min 0 -Max 100) / 100), 2)
            supplier = @("DentalPro","Suministros Med","Insumos S.A.") | Get-Random
            expirationDate = (Get-Date).AddDays((Get-Random -Min 180 -Max 900)).ToString("yyyy-MM-dd")
        }
        Invoke-Api -Method Post -Uri "$apiUrl/odontologia/inventory" -Headers $headers -Body $itemPayload | Out-Null
        Start-Sleep -Milliseconds $delayMs
    }
    $inventoryItems = Normalize-ApiArray (Invoke-Api -Method Get -Uri "$apiUrl/odontologia/inventory" -Headers $headers)
}
Write-Host "   OK - inventario disponible: $($inventoryItems.Count)" -ForegroundColor Green

# 3) Categoría contable de egresos para reflejo en resumen contable
Write-Host "`n[3/6] Verificando categoría contable de egresos..." -ForegroundColor Yellow
$categories = Normalize-ApiArray (Invoke-Api -Method Get -Uri "$apiUrl/accounting/categories" -Headers $headers)
$expenseCategory = $categories | Where-Object { $_.type -eq "Expense" } | Select-Object -First 1
if (-not $expenseCategory) {
    $expenseCategory = Invoke-Api -Method Post -Uri "$apiUrl/accounting/categories" -Headers $headers -Body @{
        name = "Egresos Operativos"
        group = "Egresos"
        type = "Expense"
        monthlyBudget = 0
        isActive = $true
    }
}
Write-Host "   OK - categoría egreso: $($expenseCategory.name)" -ForegroundColor Green

# 4) Cargar gastos y compras por 5 meses
Write-Host "`n[4/6] Creando gastos y compras por mes..." -ForegroundColor Yellow
$expenseCategories = @("Supplies","Equipment","Maintenance","Utilities","Rent","Salaries","Marketing","Professional","Other")
$expensePaymentMethods = @("Efectivo","Tarjeta","Transferencia")
$suppliers = @("DentalPro","Insumos S.A.","ClinicaPlus","Suministros Med","Proveedor Local","BioMedical Trade")

$createdExpenses = 0
$createdPurchases = 0
$createdAccountingExpenseEntries = 0
$totalExpensesAmount = 0.0
$totalPurchasesAmount = 0.0

$months = 0..4 | ForEach-Object { (Get-Date).AddMonths(-$_) }

foreach ($monthDate in $months) {
    Write-Host "   Mes: $($monthDate.ToString('MMMM yyyy'))" -ForegroundColor Cyan

    for ($i = 1; $i -le $ExpensesPerMonth; $i++) {
        $expenseDate = Get-RandomDateInMonthUtc -MonthDate $monthDate
        $category = $expenseCategories | Get-Random
        $paymentMethod = $expensePaymentMethods | Get-Random
        $amount = [Math]::Round((Get-Random -Min 25 -Max 650) + ((Get-Random -Min 0 -Max 100) / 100), 2)
        $supplier = $suppliers | Get-Random

        $expense = Invoke-Api -Method Post -Uri "$apiUrl/odontologia/gastos" -Headers $headers -Body @{
            description = "Gasto $category $($expenseDate.ToString('yyyy-MM-dd')) #$i"
            amount = $amount
            expenseDate = $expenseDate.ToString("o")
            category = $category
            paymentMethod = $paymentMethod
            invoiceNumber = "GST-$($expenseDate.ToString('yyyyMM'))-$([string]::Format('{0:0000}', $i))"
            supplier = $supplier
            notes = "Carga automatica contabilidad 5 meses"
        }
        $createdExpenses++
        $totalExpensesAmount += [double]$expense.amount

        $paymentMethodAccounting = Map-PaymentMethodToAccounting -PaymentMethod $paymentMethod
        Invoke-Api -Method Post -Uri "$apiUrl/accounting/entries" -Headers $headers -Body @{
            date = $expenseDate.ToString("o")
            type = "Expense"
            categoryId = $expenseCategory.id
            description = "Gasto operativo: $category"
            amount = $amount
            paymentMethod = $paymentMethodAccounting
            reference = "EXP-$($expense.id)"
        } | Out-Null
        $createdAccountingExpenseEntries++

        Start-Sleep -Milliseconds $delayMs
    }

    for ($j = 1; $j -le $PurchasesPerMonth; $j++) {
        $purchaseDate = Get-RandomDateInMonthUtc -MonthDate $monthDate
        $supplier = $suppliers | Get-Random
        $selectedItems = @($inventoryItems | Get-Random -Count (Get-Random -Min 1 -Max 4))
        $purchaseItems = @()

        foreach ($it in $selectedItems) {
            $qty = Get-Random -Min 2 -Max 18
            $priceBase = if ($it.unitPrice -gt 0) { [double]$it.unitPrice } else { 10.0 }
            $unitPrice = [Math]::Round(($priceBase * (Get-Random -Min 85 -Max 120) / 100), 2)
            $exp = (Get-Date).AddDays((Get-Random -Min 90 -Max 600)).ToUniversalTime().ToString("o")

            $purchaseItems += @{
                inventoryItemId = $it.id
                quantity = $qty
                unitPrice = $unitPrice
                expirationDate = $exp
            }
        }

        $purchase = Invoke-Api -Method Post -Uri "$apiUrl/odontologia/compras" -Headers $headers -Body @{
            supplier = $supplier
            invoiceNumber = "CPR-$($purchaseDate.ToString('yyyyMM'))-$([string]::Format('{0:0000}', $j))"
            purchaseDate = $purchaseDate.ToString("o")
            notes = "Compra automatica para modulo contable"
            status = "Received"
            items = $purchaseItems
        }

        $createdPurchases++
        $totalPurchasesAmount += [double]$purchase.total

        Invoke-Api -Method Post -Uri "$apiUrl/accounting/entries" -Headers $headers -Body @{
            date = $purchaseDate.ToString("o")
            type = "Expense"
            categoryId = $expenseCategory.id
            description = "Compra inventario proveedor $supplier"
            amount = [double]$purchase.total
            paymentMethod = "Transfer"
            reference = "CPR-$($purchase.id)"
        } | Out-Null
        $createdAccountingExpenseEntries++

        Start-Sleep -Milliseconds $delayMs
    }
}

# 5) Verificación por API de módulos de contabilidad
Write-Host "`n[5/6] Verificando módulos (gastos/compras/reportes)..." -ForegroundColor Yellow
$fromDate = (Get-Date).AddMonths(-5).ToString("yyyy-MM-dd")
$toDate = (Get-Date).AddDays(1).ToString("yyyy-MM-dd")

$expenses5m = Normalize-ApiArray (Invoke-Api -Method Get -Uri "$apiUrl/odontologia/gastos?startDate=$fromDate&endDate=$toDate" -Headers $headers)
$purchases5m = Normalize-ApiArray (Invoke-Api -Method Get -Uri "$apiUrl/odontologia/compras?dateFrom=$fromDate&dateTo=$toDate" -Headers $headers)
$financialReport = Invoke-Api -Method Get -Uri "$apiUrl/odontologia/reportes/financiero?startDate=$fromDate&endDate=$toDate" -Headers $headers
$accountingExpenses5m = Normalize-ApiArray (Invoke-Api -Method Get -Uri "$apiUrl/accounting/entries?type=Expense&from=$fromDate&to=$toDate&pageSize=200" -Headers $headers)

# 6) Resumen final
Write-Host "`n[6/6] Resumen final" -ForegroundColor Yellow
Write-Host "================================================" -ForegroundColor Cyan
Write-Host " CARGA CONTABLE COMPLETA FINALIZADA" -ForegroundColor Green
Write-Host "------------------------------------------------" -ForegroundColor Cyan
Write-Host " Gastos creados:                    $createdExpenses" -ForegroundColor Green
Write-Host " Compras creadas:                   $createdPurchases" -ForegroundColor Green
Write-Host " Asientos de egreso creados:        $createdAccountingExpenseEntries" -ForegroundColor Green
Write-Host " Total gastos creados:              $([Math]::Round($totalExpensesAmount, 2))" -ForegroundColor Green
Write-Host " Total compras creadas:             $([Math]::Round($totalPurchasesAmount, 2))" -ForegroundColor Green
Write-Host "------------------------------------------------" -ForegroundColor Cyan
Write-Host " Gastos API últimos 5 meses:        $($expenses5m.Count)" -ForegroundColor Cyan
Write-Host " Compras API últimos 5 meses:       $($purchases5m.Count)" -ForegroundColor Cyan
Write-Host " Egresos contables API 5 meses:     $($accountingExpenses5m.Count)" -ForegroundColor Cyan
Write-Host " Reporte financiero - Total Gastos: $($financialReport.summary.totalExpenses)" -ForegroundColor Cyan
Write-Host " Reporte financiero - Total Compras:$($financialReport.summary.totalPurchases)" -ForegroundColor Cyan
Write-Host "================================================`n" -ForegroundColor Cyan
