$ErrorActionPreference = "Continue"
$apiUrl = "http://localhost:5154/api"

Write-Host "GENERACION DE 4 MESES DE DATOS CONTABLES" -ForegroundColor Cyan
Write-Host "Octubre 2025 - Enero 2026`n" -ForegroundColor Cyan

# Login
$lO = Invoke-RestMethod -Uri "$apiUrl/auth/login" -Method Post -ContentType "application/json" -Body '{"email":"odontologo1@medicsys.com","password":"Odontologo123!"}'
$lE = Invoke-RestMethod -Uri "$apiUrl/auth/login" -Method Post -ContentType "application/json" -Body '{"email":"estudiante1@medicsys.com","password":"Estudiante123!"}'
$hO = @{Authorization="Bearer $($lO.token)"}
$hE = @{Authorization="Bearer $($lE.token)"}
Write-Host "Autenticado OK`n" -ForegroundColor Green

# Categorias
$cats = Invoke-RestMethod -Uri "$apiUrl/accounting/categories" -Headers $hO
$catI = ($cats | Where-Object {$_.type -eq "Income"})[0].id
$catG = ($cats | Where-Object {$_.type -eq "Expense"})[0].id

$nombres = @("Maria","Jose","Carlos","Ana","Luis","Carmen","Miguel","Rosa","Pedro","Laura")
$apellidos = @("Garcia","Rodriguez","Gonzalez","Fernandez","Lopez")
$tratamientos = @(@{n="Limpieza";p=45},@{n="Empaste";p=65},@{n="Extraccion";p=85})

$totalP=0;$totalH=0;$totalF=0;$totalM=0;$totalMonto=0

$meses = @(
    @{m=10;a=2025;n="Octubre";d=31;p=30;f=25;g=15;i=8},
    @{m=11;a=2025;n="Noviembre";d=30;p=35;f=30;g=18;i=10},
    @{m=12;a=2025;n="Diciembre";d=31;p=40;f=35;g=20;i=12},
    @{m=1;a=2026;n="Enero";d=31;p=45;f=40;g=22;i=15}
)

foreach ($mes in $meses) {
    Write-Host "=== $($mes.n) $($mes.a) ===" -ForegroundColor Cyan
    
    # Pacientes
    $pacs = @()
    1..$mes.p | ForEach-Object {
        $d = Get-Random -Min 1 -Max ($mes.d+1)
        $f = (Get-Date -Year $mes.a -Month $mes.m -Day $d -Hour 10).ToString("yyyy-MM-ddTHH:mm:ss")
        $p = '{"firstName":"' + $nombres[(Get-Random -Max $nombres.Length)] + '","lastName":"' + $apellidos[(Get-Random -Max $apellidos.Length)] + '","identificationNumber":"17' + (Get-Random -Min 10000000 -Max 99999999) + '","birthDate":"1990-01-01","gender":"Masculino","phone":"0999999999","email":"p' + $_ + '@mail.com","address":"Quito","emergencyContact":"0999999999","bloodType":"O+","allergies":"Ninguna","currentMedications":"Ninguno","medicalNotes":"OK","createdAt":"' + $f + '"}'
        try {
            $pac = Invoke-RestMethod -Uri "$apiUrl/patients" -Method Post -Headers $hO -Body $p -ContentType "application/json"
            $pacs += $pac
        } catch { Write-Host "." -NoNewline }
    }
    $totalP += $pacs.Length
    Write-Host "`n$($pacs.Length) pacientes OK" -ForegroundColor Green
    
    # Historias
    $hists = 0
    foreach ($pac in $pacs) {
        $d = Get-Random -Min 1 -Max ($mes.d+1)
        $f = (Get-Date -Year $mes.a -Month $mes.m -Day $d -Hour 11).ToString("yyyy-MM-ddTHH:mm:ss")
        $h = '{"patientId":"' + $pac.id + '","motivoConsulta":"Dolor","diagnostico":"Caries","tratamiento":"Empaste","observaciones":"OK","createdAt":"' + $f + '"}'
        try {
            $hist = Invoke-RestMethod -Uri "$apiUrl/clinical-histories" -Method Post -Headers $hE -Body $h -ContentType "application/json"
            Invoke-RestMethod -Uri "$apiUrl/clinical-histories/$($hist.id)/submit" -Method Post -Headers $hE | Out-Null
            $hists++
        } catch { }
    }
    $totalH += $hists
    Write-Host "$hists historias OK" -ForegroundColor Green
    
    # Facturas
    $facts = 0
    1..$mes.f | ForEach-Object {
        if ($pacs.Length -eq 0) { return }
        $pac = $pacs[(Get-Random -Max $pacs.Length)]
        $d = Get-Random -Min 1 -Max ($mes.d+1)
        $f = (Get-Date -Year $mes.a -Month $mes.m -Day $d -Hour 12).ToString("yyyy-MM-ddTHH:mm:ss")
        $t = $tratamientos[(Get-Random -Max $tratamientos.Length)]
        $fac = '{"patientId":"' + $pac.id + '","items":[{"description":"' + $t.n + '","quantity":1,"unitPrice":' + $t.p + '}],"discount":0,"notes":"Factura","paymentMethod":"Cash","createdAt":"' + $f + '"}'
        try {
            $fact = Invoke-RestMethod -Uri "$apiUrl/invoices" -Method Post -Headers $hO -Body $fac -ContentType "application/json"
            $totalMonto += $fact.total
            $facts++
        } catch { }
    }
    $totalF += $facts
    Write-Host "$facts facturas OK" -ForegroundColor Green
    
    # Gastos
    $movs = 0
    1..$mes.g | ForEach-Object {
        $d = Get-Random -Min 1 -Max ($mes.d+1)
        $f = (Get-Date -Year $mes.a -Month $mes.m -Day $d -Hour 14).ToString("yyyy-MM-ddTHH:mm:ss")
        $g = '{"description":"Material","amount":' + (Get-Random -Min 50 -Max 300) + ',"categoryId":"' + $catG + '","paymentMethod":"Cash","date":"' + $f + '"}'
        try {
            Invoke-RestMethod -Uri "$apiUrl/accounting/entries" -Method Post -Headers $hO -Body $g -ContentType "application/json" | Out-Null
            $movs++
        } catch { }
    }
    # Ingresos
    1..$mes.i | ForEach-Object {
        $d = Get-Random -Min 1 -Max ($mes.d+1)
        $f = (Get-Date -Year $mes.a -Month $mes.m -Day $d -Hour 15).ToString("yyyy-MM-ddTHH:mm:ss")
        $i = '{"description":"Consulta","amount":' + (Get-Random -Min 30 -Max 150) + ',"categoryId":"' + $catI + '","paymentMethod":"Cash","date":"' + $f + '"}'
        try {
            Invoke-RestMethod -Uri "$apiUrl/accounting/entries" -Method Post -Headers $hO -Body $i -ContentType "application/json" | Out-Null
            $movs++
        } catch { }
    }
    $totalM += $movs
    Write-Host "$movs movimientos OK`n" -ForegroundColor Green
}

Write-Host "=== RESUMEN ===" -ForegroundColor Cyan
Write-Host "Pacientes: $totalP" -ForegroundColor Green
Write-Host "Historias: $totalH" -ForegroundColor Green
Write-Host "Facturas: $totalF" -ForegroundColor Green
Write-Host "Movimientos: $totalM" -ForegroundColor Green
Write-Host "Total facturado: $([math]::Round($totalMonto,2))`n" -ForegroundColor Yellow
