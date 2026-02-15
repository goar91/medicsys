param(
    [switch]$SkipInventory,
    [switch]$SkipHistories
)

$ErrorActionPreference = "Stop"
$apiUrl = "http://localhost:5154/api"
$delayMs = 120

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Uri,
        [hashtable]$Headers,
        $Body = $null,
        [int]$MaxRetries = 3
    )

    $attempt = 0
    while ($true) {
        try {
            if ($null -eq $Body) {
                return Invoke-RestMethod -Uri $Uri -Method $Method -Headers $Headers -TimeoutSec 25
            }

            $json = $Body | ConvertTo-Json -Depth 10
            return Invoke-RestMethod -Uri $Uri -Method $Method -Headers $Headers -ContentType "application/json" -Body $json -TimeoutSec 25
        }
        catch {
            $attempt++
            if ($attempt -le $MaxRetries) {
                Start-Sleep -Seconds ([Math]::Min(2 * $attempt, 6))
                continue
            }
            throw
        }
    }
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host " MEDICSYS - CARGA OBJETIVO 100/100/5M" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# 1) Login odontologo
Write-Host "`n[1/6] Login odontologo..." -ForegroundColor Yellow
$loginOdon = @{
    email = "odontologo@medicsys.com"
    password = "Odontologo123!"
}
$resOdon = Invoke-Api -Method Post -Uri "$apiUrl/auth/login" -Body $loginOdon
$headersOdon = @{ Authorization = "Bearer $($resOdon.token)" }
Write-Host "   OK - Odontologo autenticado" -ForegroundColor Green

# 2) Login estudiante (opcional para historias)
Write-Host "`n[2/6] Login estudiante..." -ForegroundColor Yellow
$headersHist = $headersOdon
try {
    $loginEst = @{
        email = "estudiante1@medicsys.com"
        password = "Estudiante123!"
    }
    $resEst = Invoke-Api -Method Post -Uri "$apiUrl/auth/login" -Body $loginEst
    $headersHist = @{ Authorization = "Bearer $($resEst.token)" }
    Write-Host "   OK - Estudiante autenticado" -ForegroundColor Green
}
catch {
    Write-Host "   WARN - No se pudo autenticar estudiante, se usara odontologo para historias" -ForegroundColor Yellow
}

# Datos base
$nombres = @("Juan","Maria","Carlos","Ana","Jose","Laura","Pedro","Sofia","Luis","Carmen","Miguel","Isabel","Antonio","Rosa","Francisco","Elena","Manuel","Patricia","David","Lucia")
$apellidos = @("Garcia","Rodriguez","Martinez","Lopez","Gonzalez","Perez","Sanchez","Torres","Flores","Rivera","Ortiz","Mendoza")
$servicios = @("Limpieza dental","Endodoncia","Extraccion","Resina","Control general","Ortodoncia","Radiografia","Sellante","Profilaxis")
$proveedores = @("DentalPro","Insumos S.A.","ClinicaPlus","Suministros Med","Proveedor Local")

$inventoryCreated = 0
if ($SkipInventory) {
    Write-Host "`n[3/6] Inventario omitido por parametro -SkipInventory" -ForegroundColor DarkYellow
}
else {
    Write-Host "`n[3/6] Creando 100 articulos de inventario..." -ForegroundColor Yellow
    for ($i = 1; $i -le 100; $i++) {
        $item = @{
            name = "INV-$([string]::Format('{0:000}', $i)) " + (($servicios | Get-Random))
            description = "Articulo odontologico generado #$i"
            sku = "SKU5M-$([string]::Format('{0:0000}', $i))-$((Get-Random -Min 10 -Max 99))"
            quantity = Get-Random -Min 70 -Max 220
            minimumQuantity = Get-Random -Min 8 -Max 25
            unitPrice = [Math]::Round((Get-Random -Min 4 -Max 95) + ((Get-Random -Min 0 -Max 100) / 100), 2)
            supplier = $proveedores | Get-Random
            expirationDate = (Get-Date).AddDays((Get-Random -Min 120 -Max 720)).ToString("yyyy-MM-dd")
        }

        try {
            Invoke-Api -Method Post -Uri "$apiUrl/odontologia/inventory" -Headers $headersOdon -Body $item | Out-Null
            $inventoryCreated++
        }
        catch {
            Write-Host "   WARN - Fallo item ${i}: $($_.Exception.Message)" -ForegroundColor DarkYellow
        }

        if (($i % 20) -eq 0) {
            Write-Host "   $i/100 procesados..." -ForegroundColor Gray
        }
        Start-Sleep -Milliseconds $delayMs
    }
    Write-Host "   OK - Inventario creado: $inventoryCreated" -ForegroundColor Green
}

# Obtener pacientes para enlazar historias
$patients = @()
try {
    $patients = Invoke-Api -Method Get -Uri "$apiUrl/patients" -Headers $headersOdon
} catch {}

$historiesCreated = 0
if ($SkipHistories) {
    Write-Host "`n[4/6] Historias omitidas por parametro -SkipHistories" -ForegroundColor DarkYellow
}
else {
    Write-Host "`n[4/6] Creando 100 historias clinicas..." -ForegroundColor Yellow
    for ($i = 1; $i -le 100; $i++) {
        $nom = $nombres | Get-Random
        $ape = $apellidos | Get-Random
        $patientId = $null
        if ($patients.Count -gt 0) {
            $patientId = ($patients | Get-Random).id
        }

        $historyPayload = @{
            patientId = $patientId
            data = @{
                personal = @{
                    firstName = $nom
                    lastName = $ape
                    idNumber = "17" + (Get-Random -Min 10000000 -Max 99999999)
                    dateOfBirth = (Get-Date).AddYears(-(Get-Random -Min 18 -Max 75)).ToString("yyyy-MM-dd")
                    gender = ("M","F") | Get-Random
                    phone = "09" + (Get-Random -Min 10000000 -Max 99999999)
                }
                consulta = @{
                    motivo = $servicios | Get-Random
                    diagnostico = ("Caries leve","Gingivitis","Control","Dolor dental","Sensibilidad","Trauma") | Get-Random
                    tratamiento = ("Limpieza","Resina","Analgesicos","Endodoncia","Seguimiento","Cirugia menor") | Get-Random
                }
                observaciones = "Historia automatica 100/100/5M #$i"
                fecha = (Get-Date).AddDays((Get-Random -Min -150 -Max 0)).ToString("yyyy-MM-dd")
            }
        }

        try {
            Invoke-Api -Method Post -Uri "$apiUrl/clinical-histories" -Headers $headersHist -Body $historyPayload | Out-Null
            $historiesCreated++
        }
        catch {
            Write-Host "   WARN - Fallo historia ${i}: $($_.Exception.Message)" -ForegroundColor DarkYellow
        }

        if (($i % 20) -eq 0) {
            Write-Host "   $i/100 procesadas..." -ForegroundColor Gray
        }
        Start-Sleep -Milliseconds $delayMs
    }
    Write-Host "   OK - Historias creadas: $historiesCreated" -ForegroundColor Green
}

# 5) Consumos (Kardex salida) + contabilidad (egreso) por 5 meses
Write-Host "`n[5/6] Generando consumos de inventario por 5 meses y reflejo contable..." -ForegroundColor Yellow
$expenseCategory = $null
$categories = @()
try {
    $categories = @(Invoke-Api -Method Get -Uri "$apiUrl/accounting/categories" -Headers $headersOdon)
}
catch {
    throw "No fue posible obtener categorias contables: $($_.Exception.Message)"
}

$expenseCategory = $categories | Where-Object { $_.type -eq "Expense" } | Select-Object -First 1
if (-not $expenseCategory) {
    Write-Host "   INFO - No existe categoria Expense. Creando categoria por defecto..." -ForegroundColor Yellow
    $expenseCategory = Invoke-Api -Method Post -Uri "$apiUrl/accounting/categories" -Headers $headersOdon -Body @{
        name = "Consumos de inventario"
        group = "Egresos"
        type = "Expense"
        monthlyBudget = 0
        isActive = $true
    }
}

$inventoryItems = Invoke-Api -Method Get -Uri "$apiUrl/odontologia/kardex/items" -Headers $headersOdon
if (-not $inventoryItems -or $inventoryItems.Count -eq 0) {
    throw "No hay items de inventario disponibles para consumos."
}

$stockById = @{}
$nameById = @{}
$priceById = @{}
foreach ($item in $inventoryItems) {
    $stockById[$item.id] = [int]$item.quantity
    $nameById[$item.id] = [string]$item.name
    $priceById[$item.id] = [decimal]$item.unitPrice
}

$consumptionsCreated = 0
$accountingEntriesCreated = 0
$totalConsumedAmount = 0.0

$months = 0..4 | ForEach-Object { (Get-Date).AddMonths(-$_) }
foreach ($monthDate in $months) {
    $year = $monthDate.Year
    $month = $monthDate.Month
    $daysInMonth = [DateTime]::DaysInMonth($year, $month)
    Write-Host "   Mes: $($monthDate.ToString('MMMM yyyy'))" -ForegroundColor Cyan

    for ($i = 1; $i -le 24; $i++) {
        $eligible = @($stockById.Keys | Where-Object { $stockById[$_] -gt 0 })
        if ($eligible.Count -eq 0) {
            Write-Host "   WARN - Sin stock restante para mas consumos." -ForegroundColor Yellow
            break
        }

        $itemId = $eligible | Get-Random
        $availableStock = [int]$stockById[$itemId]
        $qty = Get-Random -Min 1 -Max ([Math]::Min(4, $availableStock) + 1)
        $moveDay = Get-Random -Min 1 -Max ($daysInMonth + 1)
        $moveDate = (Get-Date -Year $year -Month $month -Day $moveDay -Hour (Get-Random -Min 8 -Max 18) -Minute (Get-Random -Min 0 -Max 59) -Second 0).ToUniversalTime()

        $exitPayload = @{
            inventoryItemId = $itemId
            quantity = $qty
            unitPrice = [decimal]$priceById[$itemId]
            movementDate = $moveDate.ToString("o")
            reference = "CONSUMO-$($year)$([string]::Format('{0:00}', $month))-$i"
            notes = "Consumo clinico mensual"
        }

        try {
            $exitResponse = Invoke-Api -Method Post -Uri "$apiUrl/odontologia/kardex/movements/exit" -Headers $headersOdon -Body $exitPayload
            $consumptionsCreated++

            $newQty = [int]$exitResponse.item.quantity
            $stockById[$itemId] = $newQty
            $cost = [decimal]$exitResponse.movement.totalCost
            if ($cost -le 0) {
                $cost = [Math]::Round(([decimal]$priceById[$itemId] * $qty), 2)
            }

            $entryPayload = @{
                date = $moveDate.ToString("o")
                type = "Expense"
                categoryId = $expenseCategory.id
                description = "Consumo inventario: $($nameById[$itemId])"
                amount = $cost
                paymentMethod = "Cash"
                reference = "KDX-$($exitResponse.movement.id)"
            }

            Invoke-Api -Method Post -Uri "$apiUrl/accounting/entries" -Headers $headersOdon -Body $entryPayload | Out-Null
            $accountingEntriesCreated++
            $totalConsumedAmount += $cost
        }
        catch {
            Write-Host "   WARN - Fallo consumo/contable mes ${month} item ${itemId}: $($_.Exception.Message)" -ForegroundColor DarkYellow
        }

        Start-Sleep -Milliseconds $delayMs
    }
}

# 6) Verificacion rapida
Write-Host "`n[6/6] Verificando datos..." -ForegroundColor Yellow
$entriesCheck = Invoke-Api -Method Get -Uri "$apiUrl/accounting/entries?page=1&pageSize=5&type=Expense" -Headers $headersOdon
$inventoryCheck = Invoke-Api -Method Get -Uri "$apiUrl/odontologia/inventory?page=1&pageSize=1" -Headers $headersOdon
$historiesCheck = Invoke-Api -Method Get -Uri "$apiUrl/clinical-histories?page=1&pageSize=1" -Headers $headersHist

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host " CARGA FINALIZADA" -ForegroundColor Green
Write-Host "------------------------------------------------" -ForegroundColor Cyan
Write-Host " Inventario nuevo:          $inventoryCreated" -ForegroundColor Green
Write-Host " Historias nuevas:          $historiesCreated" -ForegroundColor Green
Write-Host " Consumos (kardex salida):  $consumptionsCreated" -ForegroundColor Green
Write-Host " Asientos contables gasto:  $accountingEntriesCreated" -ForegroundColor Green
Write-Host " Monto total consumido:     $([Math]::Round($totalConsumedAmount, 2))" -ForegroundColor Green
Write-Host "------------------------------------------------" -ForegroundColor Cyan
Write-Host " Muestra inventario API:    $($inventoryCheck.Count) item(s) recibidos (paginado)" -ForegroundColor Gray
Write-Host " Muestra historias API:     $($historiesCheck.Count) historia(s) recibidas (paginado)" -ForegroundColor Gray
Write-Host " Muestra egresos API:       $($entriesCheck.Count) movimientos recibidos (paginado)" -ForegroundColor Gray
Write-Host "================================================`n" -ForegroundColor Cyan
