# Script simple para poblar datos - MEDICSYS
# Ejecutar después de autenticarse

$script:pIds = @()
$script:iIds = @()

Write-Host "`nPACIENTES:" -ForegroundColor Cyan

$patients = @(
    @'{"firstName":"Maria","lastName":"Gonzalez Perez","idNumber":"0912345678","dateOfBirth":"1985-03-15","gender":"Femenino","address":"Av 9 de Octubre 123","phone":"0987654321","email":"maria.g@test.com","allergies":"Penicilina","bloodType":"O+"}
'@,
    @'{"firstName":"Carlos","lastName":"Rodriguez Silva","idNumber":"0923456789","dateOfBirth":"1990-07-22","gender":"Masculino","address":"Calle Padre Solano 456","phone":"0998765432","email":"carlos.r@test.com","medications":"Losartan 50mg","bloodType":"A+"}
'@,
    @'{"firstName":"Ana","lastName":"Martinez Lopez","idNumber":"0934567890","dateOfBirth":"1978-11-30","gender":"Femenino","address":"Av Las Americas 789","phone":"0976543210","email":"ana.m@test.com","allergies":"Latex","medications":"Metformina 850mg","diseases":"Diabetes Tipo 2","bloodType":"B+"}
'@,
    @'{"firstName":"Roberto","lastName":"Fernandez Castro","idNumber":"0945678901","dateOfBirth":"1995-05-18","gender":"Masculino","address":"Urdesa Central","phone":"0965432109","email":"roberto.f@test.com","bloodType":"AB+"}
'@,
    @'{"firstName":"Laura","lastName":"Sanchez Moreno","idNumber":"0956789012","dateOfBirth":"1988-09-25","gender":"Femenino","address":"Kennedy Norte","phone":"0954321098","email":"laura.s@test.com","allergies":"Ibuprofeno","bloodType":"O-"}
'@
)

foreach ($p in $patients) {
    try {
        $r = Invoke-RestMethod -Uri "$baseUrl/patients" -Method Post -Body $p -Headers $headers
        $script:pIds += $r.id
        Write-Host "  ✓ Paciente creado" -ForegroundColor Green
        Start-Sleep -Milliseconds 300
    } catch {
        Write-Host "  ✗ Error" -ForegroundColor Red
    }
}

Write-Host "`nINVENTARIO:" -ForegroundColor Cyan

$items = @(
    @'{"name":"Guantes Latex Caja x100","sku":"GLT-M-100","initialQuantity":50,"minimumQuantity":10,"unitPrice":8.50,"supplier":"DentalSupply SA","location":"Estante A1"}
'@,
    @'{"name":"Anestesia Lidocaina 2%","sku":"LIDO-2-50","initialQuantity":30,"minimumQuantity":5,"unitPrice":2.75,"supplier":"Pharma Dental","location":"Refrigerador 1","expirationDate":"2026-12-31"}
'@,
    @'{"name":"Resina Compuesta A2","sku":"RES-A2-4G","initialQuantity":20,"minimumQuantity":3,"unitPrice":45.00,"supplier":"3M Dental","location":"Estante B2"}
'@,
    @'{"name":"Agujas Dentales 27G","sku":"AGU-27G-100","initialQuantity":100,"minimumQuantity":20,"unitPrice":0.35,"supplier":"DentalSupply SA","location":"Estante A2"}
'@,
    @'{"name":"Algodon en Rollos","sku":"ALG-ROL-500","initialQuantity":80,"minimumQuantity":15,"unitPrice":5.20,"supplier":"Dental Imports","location":"Estante C1"}
'@,
    @'{"name":"Cepillos Dentales","sku":"CEP-ADU-12","initialQuantity":60,"minimumQuantity":10,"unitPrice":1.50,"supplier":"Colgate Ecuador","location":"Estante D1"}
'@,
    @'{"name":"Hilo Dental Menta","sku":"HIL-MEN-50","initialQuantity":45,"minimumQuantity":8,"unitPrice":2.00,"supplier":"Oral-B Ecuador","location":"Estante D2"}
'@,
    @'{"name":"Mascarillas Quirurgicas","sku":"MAS-3C-50","initialQuantity":200,"minimumQuantity":30,"unitPrice":0.25,"supplier":"MedSupply","location":"Estante A3"}
'@
)

foreach ($it in $items) {
    try {
        $r = Invoke-RestMethod -Uri "$baseUrl/odontologia/kardex/items" -Method Post -Body $it -Headers $headers
        $script:iIds += $r.id
        Write-Host "  ✓ Item creado" -ForegroundColor Green
        Start-Sleep -Milliseconds 300
    } catch {
        Write-Host "  ✗ Error" -ForegroundColor Red
    }
}

Write-Host "`nMOVIMIENTOS:" -ForegroundColor Cyan

if ($script:iIds.Count -gt 2) {
    $f15 = (Get-Date).AddDays(-15).ToString("yyyy-MM-dd")
    $f10 = (Get-Date).AddDays(-10).ToString("yyyy-MM-dd")
    
    for ($i = 0; $i -lt 3; $i++) {
        $eJson = @"
{"inventoryItemId":"$($script:iIds[$i])","quantity":25,"unitPrice":10.50,"movementDate":"$f15","reference":"Compra quincenal","notes":"Reabastecimiento"}
"@
        try {
            Invoke-RestMethod -Uri "$baseUrl/odontologia/kardex/entry" -Method Post -Body $eJson -Headers $headers | Out-Null
            Write-Host "  ✓ Entrada" -ForegroundColor Green
            Start-Sleep -Milliseconds 200
        } catch {}
    }
    
    for ($i = 0; $i -lt 3; $i++) {
        $sJson = @"
{"inventoryItemId":"$($script:iIds[$i])","quantity":5,"unitPrice":10.00,"movementDate":"$f10","reference":"Uso clinico","notes":"Consumo"}
"@
        try {
            Invoke-RestMethod -Uri "$baseUrl/odontologia/kardex/exit" -Method Post -Body $sJson -Headers $headers | Out-Null
            Write-Host "  ✓ Salida" -ForegroundColor Green
            Start-Sleep -Milliseconds 200
        } catch {}
    }
}

Write-Host "`nHISTORIAS CLÍNICAS:" -ForegroundColor Cyan

if ($script:pIds.Count -gt 0) {
    $f20 = (Get-Date).AddDays(-20).ToString("yyyy-MM-dd")
    $f12 = (Get-Date).AddDays(-12).ToString("yyyy-MM-dd")
    
    $hc1 = @"
{"patientId":"$($script:pIds[0])","data":{"motivo":"Limpieza dental","anamnesis":"Sensibilidad leve","examenClinico":"Calculo dental moderado","diagnostico":"Gingivitis cronica","tratamiento":"Profilaxis dental","fecha":"$f20"}}
"@
    try {
        Invoke-RestMethod -Uri "$baseUrl/clinical-histories" -Method Post -Body $hc1 -Headers $headers | Out-Null
        Write-Host "  ✓ HC 1" -ForegroundColor Green
    } catch {}
    
    if ($script:pIds.Count -gt 1) {
        $hc2 = @"
{"patientId":"$($script:pIds[1])","data":{"motivo":"Dolor muela","anamnesis":"Dolor intermitente","examenClinico":"Tercer molar erupcionado","diagnostico":"Pericoronitis aguda","tratamiento":"Antibiotico","fecha":"$f12"}}
"@
        try {
            Invoke-RestMethod -Uri "$baseUrl/clinical-histories" -Method Post -Body $hc2 -Headers $headers | Out-Null
            Write-Host "  ✓ HC 2" -ForegroundColor Green
        } catch {}
    }
}

Write-Host "`nGASTOS:" -ForegroundColor Cyan

$gastos = @(
    @{d=(Get-Date).AddDays(-25).ToString("yyyy-MM-dd");c="Servicios Basicos";a=145.50;desc="Luz electrica";pm="Transferencia"},
    @{d=(Get-Date).AddDays(-20).ToString("yyyy-MM-dd");c="Salarios";a=800.00;desc="Salario asistente";pm="Transferencia"},
    @{d=(Get-Date).AddDays(-15).ToString("yyyy-MM-dd");c="Insumos";a=350.00;desc="Materiales dentales";pm="Transferencia"},
    @{d=(Get-Date).AddDays(-10).ToString("yyyy-MM-dd");c="Servicios Profesionales";a=180.00;desc="Honorarios contador";pm="Transferencia"},
    @{d=(Get-Date).AddDays(-5).ToString("yyyy-MM-dd");c="Insumos";a=95.50;desc="Material limpieza";pm="Efectivo"}
)

foreach ($g in $gastos) {
    $gJson = @"
{"category":"$($g.c)","amount":$($g.a),"description":"$($g.desc)","date":"$($g.d)","paymentMethod":"$($g.pm)"}
"@
    try {
        Invoke-RestMethod -Uri "$baseUrl/odontologia/gastos" -Method Post -Body $gJson -Headers $headers | Out-Null
        Write-Host "  ✓ $($g.desc)" -ForegroundColor Green
        Start-Sleep -Milliseconds 200
    } catch {}
}

Write-Host "`n══════════════════════════════════" -ForegroundColor Cyan
Write-Host "✓ COMPLETADO" -ForegroundColor Green
Write-Host "Pacientes: $($script:pIds.Count)" -ForegroundColor Yellow
Write-Host "Items: $($script:iIds.Count)" -ForegroundColor Yellow
Write-Host "══════════════════════════════════" -ForegroundColor Cyan
