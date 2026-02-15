$ErrorActionPreference = "Continue"
$apiUrl = "http://localhost:5154/api"

Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "  GENERADOR DE HISTORIAS CLÍNICAS" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Cyan

Write-Host ""
Write-Host "[1/6] Autenticando..." -ForegroundColor Yellow

# Login Odontólogo
$loginOd = @{ email = "odontologo1@medicsys.com"; password = "Odontologo123!" } | ConvertTo-Json
$responseOd = Invoke-RestMethod -Uri "$apiUrl/auth/login" -Method Post -Body $loginOd -ContentType "application/json"
$headersOd = @{ "Authorization" = "Bearer $($responseOd.token)"; "Content-Type" = "application/json" }

# Login Estudiante
$loginEst = @{ email = "estudiante1@medicsys.com"; password = "Estudiante123!" } | ConvertTo-Json
$responseEst = Invoke-RestMethod -Uri "$apiUrl/auth/login" -Method Post -Body $loginEst -ContentType "application/json"
$headersEst = @{ "Authorization" = "Bearer $($responseEst.token)"; "Content-Type" = "application/json" }

Write-Host "OK - Autenticación exitosa" -ForegroundColor Green

# Obtener categorías
Write-Host ""
Write-Host "[2/6] Obteniendo categorías..." -ForegroundColor Yellow
$categorias = Invoke-RestMethod -Uri "$apiUrl/accounting/categories" -Method Get -Headers $headersOd
$catIngreso = ($categorias | Where-Object { $_.group -eq "Income" })[0]
$catGasto = ($categorias | Where-Object { $_.group -eq "Expense" })[0]
Write-Host "OK - Categorías obtenidas" -ForegroundColor Green

# Datos
$nombres = @("Juan","María","Carlos","Ana","José","Laura","Pedro","Sofía","Luis","Carmen","Miguel","Isabel","Antonio","Rosa","Francisco","Elena","Manuel","Patricia","David","Lucía")
$apellidos = @("García","Rodríguez","Martínez","López","González","Pérez","Sánchez","Torres","Flores","Rivera")
$tratamientos = @(
    @{n="Limpieza";p=45}, @{n="Resina";p=55}, @{n="Extracción";p=35},
    @{n="Endodoncia";p=150}, @{n="Corona";p=250}, @{n="Ortodoncia";p=120}
)

Write-Host ""
Write-Host "[3/6] Creando 50 pacientes..." -ForegroundColor Yellow
$pacientes = @()
1..50 | ForEach-Object {
    $i = $_
    $nom = $nombres | Get-Random
    $ape1 = $apellidos | Get-Random
    $ape2 = $apellidos | Get-Random
    $pacData = @{
        firstName = $nom
        lastName = "$ape1 $ape2"
        idNumber = "17" + (Get-Random -Min 10000000 -Max 99999999)
        dateOfBirth = (Get-Date).AddYears(-(Get-Random -Min 18 -Max 70)).ToString("yyyy-MM-dd")
        gender = ("M","F") | Get-Random
        address = "Av. Principal #$i-" + (Get-Random -Min 100 -Max 999)
        phone = "09" + (Get-Random -Min 10000000 -Max 99999999)
        email = "$($nom.ToLower()).$($ape1.ToLower())@email.com"
        bloodType = ("O+","A+","B+","AB+") | Get-Random
    } | ConvertTo-Json
    $pac = Invoke-RestMethod -Uri "$apiUrl/patients" -Method Post -Headers $headersOd -Body $pacData
    $pacientes += $pac
    if ($i % 10 -eq 0) { Write-Host "  $i/50 pacientes creados..." -ForegroundColor Gray }
}
Write-Host "OK - 50 pacientes creados" -ForegroundColor Green

Write-Host ""
Write-Host "[4/6] Creando 50 historias clínicas..." -ForegroundColor Yellow
$historias = @()
0..49 | ForEach-Object {
    $i = $_
    $histData = @{
        patientId = $pacientes[$i].id
        data = (@{
            motivoConsulta = "Dolor dental"
            diagnostico = "Caries dental"
            tratamiento = "Tratamiento realizado"
        } | ConvertTo-Json)
    } | ConvertTo-Json
    $hist = Invoke-RestMethod -Uri "$apiUrl/clinical-histories" -Method Post -Headers $headersEst -Body $histData
    $submitData = @{} | ConvertTo-Json
    Invoke-RestMethod -Uri "$apiUrl/clinical-histories/$($hist.id)/submit" -Method Post -Headers $headersEst -Body $submitData | Out-Null
    $historias += $hist
    if (($i+1) % 10 -eq 0) { Write-Host "  $($i+1)/50 historias creadas..." -ForegroundColor Gray }
}
Write-Host "OK - 50 historias creadas" -ForegroundColor Green

Write-Host ""
Write-Host "[5/6] Creando 50 facturas..." -ForegroundColor Yellow
$facturas = @()
0..49 | ForEach-Object {
    $i = $_
    $pac = $pacientes[$i]
    $numItems = Get-Random -Min 1 -Max 4
    $items = @()
    1..$numItems | ForEach-Object {
        $trat = $tratamientos | Get-Random
        $items += @{
            description = $trat.n
            quantity = 1
            unitPrice = $trat.p
            discountPercent = (0,5,10) | Get-Random
        }
    }
    $facData = @{
        customerIdentification = $pac.idNumber
        customerName = "$($pac.firstName) $($pac.lastName)"
        customerAddress = $pac.address
        customerPhone = $pac.phone
        customerEmail = $pac.email
        observations = "Tratamiento dental"
        items = $items
        paymentMethod = ("Cash","Card","Transfer") | Get-Random
    } | ConvertTo-Json -Depth 5
    $fac = Invoke-RestMethod -Uri "$apiUrl/invoices" -Method Post -Headers $headersOd -Body $facData
    $facturas += $fac
    if (($i+1) % 10 -eq 0) { Write-Host "  $($i+1)/50 facturas creadas..." -ForegroundColor Gray }
}
Write-Host "OK - 50 facturas creadas" -ForegroundColor Green

Write-Host ""
Write-Host "[6/6] Creando movimientos contables..." -ForegroundColor Yellow
$movimientos = 0
1..20 | ForEach-Object {
    $gastoData = @{
        date = (Get-Date).AddDays(-(Get-Random -Min 1 -Max 60)).ToString("yyyy-MM-dd")
        type = "Expense"
        categoryId = $catGasto.id
        description = ("Material dental","Servicios","Mantenimiento","Insumos") | Get-Random
        amount = Get-Random -Min 50 -Max 500
        paymentMethod = ("Cash","Transfer") | Get-Random
        reference = "REF-" + (Get-Random -Min 1000 -Max 9999)
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$apiUrl/accounting/entries" -Method Post -Headers $headersOd -Body $gastoData | Out-Null
    $movimientos++
}
1..10 | ForEach-Object {
    $ingData = @{
        date = (Get-Date).AddDays(-(Get-Random -Min 1 -Max 60)).ToString("yyyy-MM-dd")
        type = "Income"
        categoryId = $catIngreso.id
        description = ("Consulta","Radiografía","Certificado") | Get-Random
        amount = Get-Random -Min 25 -Max 150
        paymentMethod = ("Cash","Transfer") | Get-Random
        reference = "ING-" + (Get-Random -Min 1000 -Max 9999)
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$apiUrl/accounting/entries" -Method Post -Headers $headersOd -Body $ingData | Out-Null
    $movimientos++
}
Write-Host "OK - $movimientos movimientos creados" -ForegroundColor Green

$totalFacturado = ($facturas | Measure-Object -Property total -Sum).Sum
Write-Host ""
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "  RESUMEN" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "Pacientes:     50" -ForegroundColor White
Write-Host "Historias:     50" -ForegroundColor White
Write-Host "Facturas:      50" -ForegroundColor White
Write-Host "Movimientos:   $movimientos" -ForegroundColor White
Write-Host "Total:         $$([math]::Round($totalFacturado, 2))" -ForegroundColor Cyan
Write-Host ""
Write-Host "Proceso completado!" -ForegroundColor Green
Write-Host "http://localhost:4200" -ForegroundColor Gray
Write-Host ""
