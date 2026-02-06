$ErrorActionPreference = "Continue"
$apiUrl = "http://localhost:5154/api"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "   MEDICSYS - DATOS 5 MESES (ODONTOLOGO)" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Autenticacion
Write-Host "[1/4] Autenticando..." -ForegroundColor Yellow
$loginOdon = @{ email = "odontologo@medicsys.com"; password = "Odontologo123!" } | ConvertTo-Json
$resOdon = Invoke-RestMethod -Uri "$apiUrl/auth/login" -Method Post -ContentType "application/json" -Body $loginOdon
$headersOdon = @{ Authorization = "Bearer $($resOdon.token)"; "Content-Type" = "application/json" }
Write-Host "   OK - Login odontologo" -ForegroundColor Green

# Categorias contables
Write-Host "`n[2/4] Obteniendo categorias contables..." -ForegroundColor Yellow
$cats = Invoke-RestMethod -Uri "$apiUrl/accounting/categories" -Headers $headersOdon
$incomeCat = ($cats | Where-Object { $_.group -eq "Income" -or $_.type -eq "Income" } | Select-Object -First 1)
$expenseCat = ($cats | Where-Object { $_.group -eq "Expense" -or $_.type -eq "Expense" } | Select-Object -First 1)
if (-not $incomeCat) { $incomeCat = $cats | Select-Object -First 1 }
if (-not $expenseCat) { $expenseCat = $cats | Select-Object -Last 1 }
Write-Host "   OK - Categorias: Income=$($incomeCat.name) Expense=$($expenseCat.name)" -ForegroundColor Green

# Datos base
$nombres = @("Juan","Maria","Carlos","Ana","Jose","Laura","Pedro","Sofia","Luis","Carmen","Miguel","Isabel","Antonio","Rosa","Francisco","Elena","Manuel","Patricia","David","Lucia")
$apellidos = @("Garcia","Rodriguez","Martinez","Lopez","Gonzalez","Perez","Sanchez","Torres","Flores","Rivera")
$tratamientos = @(
    @{ n = "Limpieza"; p = 45 },
    @{ n = "Resina"; p = 55 },
    @{ n = "Extraccion"; p = 35 },
    @{ n = "Endodoncia"; p = 150 },
    @{ n = "Corona"; p = 250 },
    @{ n = "Ortodoncia"; p = 120 }
)

# Crear pacientes (hasta 50)
Write-Host "`n[3/4] Verificando pacientes..." -ForegroundColor Yellow
$pacientes = @()
try {
    $pacientes = Invoke-RestMethod -Uri "$apiUrl/patients" -Headers $headersOdon
} catch {
    $pacientes = @()
}

$targetPatients = 50
$missing = $targetPatients - $pacientes.Count
if ($missing -le 0) {
    Write-Host "   OK - Ya existen $($pacientes.Count) pacientes" -ForegroundColor Green
} else {
    Write-Host "   Creando $missing pacientes..." -ForegroundColor Yellow
    $created = 0
    $usedIds = @{}
    $pacientes | ForEach-Object { if ($_.idNumber) { $usedIds[$_.idNumber] = $true } }
    while ($created -lt $missing) {
        $i = $pacientes.Count + 1
        $nom = $nombres | Get-Random
        $ape1 = $apellidos | Get-Random
        $ape2 = $apellidos | Get-Random
        $idNumber = "17" + (Get-Random -Min 10000000 -Max 99999999)
        if ($usedIds.ContainsKey($idNumber)) { continue }
        $usedIds[$idNumber] = $true
        $email = "$($nom.ToLower()).$($ape1.ToLower())$i@medicsys.demo"
    $pacData = @{
        firstName = $nom
        lastName = "$ape1 $ape2"
        idNumber = $idNumber
        dateOfBirth = (Get-Date).AddYears(-(Get-Random -Min 18 -Max 70)).ToString("yyyy-MM-dd")
        gender = ("M","F") | Get-Random
        address = "Av. Principal #$i-" + (Get-Random -Min 100 -Max 999)
        phone = "09" + (Get-Random -Min 10000000 -Max 99999999)
        email = $email
        bloodType = ("O+","A+","B+","AB+") | Get-Random
    } | ConvertTo-Json

        try {
            $pac = Invoke-RestMethod -Uri "$apiUrl/patients" -Method Post -Headers $headersOdon -Body $pacData
            $pacientes += $pac
            $created++
            if ($created % 10 -eq 0) {
                Write-Host "   $created/$missing pacientes creados..." -ForegroundColor Gray
            }
        } catch {
            Write-Host "." -NoNewline
        }
    }
    Write-Host "`n   OK - $created pacientes creados (total: $($pacientes.Count))" -ForegroundColor Green
}

# Movimientos contables 5 meses
Write-Host "`n[4/4] Creando movimientos contables (5 meses)..." -ForegroundColor Yellow
$now = Get-Date
$months = 0..4 | ForEach-Object {
    $d = $now.AddMonths(-$_)
    [PSCustomObject]@{
        Year = $d.Year
        Month = $d.Month
        Name = $d.ToString("MMMM yyyy")
        Days = [DateTime]::DaysInMonth($d.Year, $d.Month)
    }
}

$totalMovs = 0
foreach ($m in $months) {
    Write-Host "   Mes: $($m.Name)" -ForegroundColor Cyan
    # Gastos
    1..8 | ForEach-Object {
        $day = Get-Random -Min 1 -Max ($m.Days + 1)
        $date = (Get-Date -Year $m.Year -Month $m.Month -Day $day).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        $gastoData = @{
            date = $date
            type = "Expense"
            categoryId = $expenseCat.id
            description = ("Material dental","Servicios","Mantenimiento","Insumos","Equipos") | Get-Random
            amount = Get-Random -Min 50 -Max 500
            paymentMethod = ("Cash","Transfer","Card") | Get-Random
            reference = "EXP-$($m.Month)-$((Get-Random -Min 1000 -Max 9999))"
        } | ConvertTo-Json
        try {
            Invoke-RestMethod -Uri "$apiUrl/accounting/entries" -Method Post -Headers $headersOdon -Body $gastoData | Out-Null
            $totalMovs++
        } catch { }
    }
    # Ingresos
    1..5 | ForEach-Object {
        $day = Get-Random -Min 1 -Max ($m.Days + 1)
        $date = (Get-Date -Year $m.Year -Month $m.Month -Day $day).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        $ingData = @{
            date = $date
            type = "Income"
            categoryId = $incomeCat.id
            description = ("Consulta","Radiografia","Certificado","Control") | Get-Random
            amount = Get-Random -Min 25 -Max 200
            paymentMethod = ("Cash","Transfer","Card") | Get-Random
            reference = "ING-$($m.Month)-$((Get-Random -Min 1000 -Max 9999))"
        } | ConvertTo-Json
        try {
            Invoke-RestMethod -Uri "$apiUrl/accounting/entries" -Method Post -Headers $headersOdon -Body $ingData | Out-Null
            $totalMovs++
        } catch { }
    }
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "RESUMEN" -ForegroundColor Cyan
Write-Host "Pacientes creados: $($pacientes.Count)" -ForegroundColor Green
Write-Host "Movimientos:       $totalMovs (5 meses)" -ForegroundColor Green
Write-Host "================================================`n" -ForegroundColor Cyan
