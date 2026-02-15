# Crear datos de prueba - MEDICSYS
$baseUrl = "http://localhost:5154/api"

# Variables globales para almacenar IDs
$script:patientIds = @()
$script:itemIds = @()

# Ya tenemos $headers y $token del comando anterior

Write-Host "`n=== CREANDO PACIENTES ===" -ForegroundColor Cyan

# Paciente 1
try {
    $p1 = '{"firstName":"Maria","lastName":"Gonzalez Perez","idNumber":"0912345678","dateOfBirth":"1985-03-15","gender":"Femenino","address":"Av 9 de Octubre 123","phone":"0987654321","email":"maria.g@email.com","allergies":"Penicilina","bloodType":"O+"}' 
    $r1 = Invoke-RestMethod -Uri "$baseUrl/patients" -Method Post -Body $p1 -Headers $headers
    $script:patientIds += $r1.id
    Write-Host "  ✓ Maria Gonzalez Perez" -ForegroundColor Green
    Start-Sleep -Milliseconds 300
} catch { Write-Host "  ✗ Error: $_" -ForegroundColor Yellow }

# Paciente 2
try {
    $p2 = '{"firstName":"Carlos","lastName":"Rodriguez Silva","idNumber":"0923456789","dateOfBirth":"1990-07-22","gender":"Masculino","address":"Calle Padre Solano 456","phone":"0998765432","email":"carlos.r@email.com","medications":"Losartan 50mg","bloodType":"A+"}' 
    $r2 = Invoke-RestMethod -Uri "$baseUrl/patients" -Method Post -Body $p2 -Headers $headers
    $script:patientIds += $r2.id
    Write-Host "  ✓ Carlos Rodriguez Silva" -ForegroundColor Green
    Start-Sleep -Milliseconds 300
} catch { Write-Host "  ✗ Error" -ForegroundColor Yellow }

# Paciente 3
try {
    $p3 = '{"firstName":"Ana","lastName":"Martinez Lopez","idNumber":"0934567890","dateOfBirth":"1978-11-30","gender":"Femenino","address":"Av Las Americas 789","phone":"0976543210","email":"ana.m@email.com","allergies":"Latex","medications":"Metformina 850mg","diseases":"Diabetes Tipo 2","bloodType":"B+"}' 
    $r3 = Invoke-RestMethod -Uri "$baseUrl/patients" -Method Post -Body $p3 -Headers $headers
    $script:patientIds += $r3.id
    Write-Host "  ✓ Ana Martinez Lopez" -ForegroundColor Green
    Start-Sleep -Milliseconds 300
} catch { Write-Host "  ✗ Error" -ForegroundColor Yellow }

# Paciente 4
try {
    $p4 = '{"firstName":"Roberto","lastName":"Fernandez Castro","idNumber":"0945678901","dateOfBirth":"1995-05-18","gender":"Masculino","address":"Urdesa Central","phone":"0965432109","email":"roberto.f@email.com","bloodType":"AB+"}' 
    $r4 = Invoke-RestMethod -Uri "$baseUrl/patients" -Method Post -Body $p4 -Headers $headers
    $script:patientIds += $r4.id
    Write-Host "  ✓ Roberto Fernandez Castro" -ForegroundColor Green
    Start-Sleep -Milliseconds 300
} catch { Write-Host "  ✗ Error" -ForegroundColor Yellow }

# Paciente 5
try {
    $p5 = '{"firstName":"Laura","lastName":"Sanchez Moreno","idNumber":"0956789012","dateOfBirth":"1988-09-25","gender":"Femenino","address":"Kennedy Norte","phone":"0954321098","email":"laura.s@email.com","allergies":"Ibuprofeno","bloodType":"O-"}' 
    $r5 = Invoke-RestMethod -Uri "$baseUrl/patients" -Method Post -Body $p5 -Headers $headers
    $script:patientIds += $r5.id
    Write-Host "  ✓ Laura Sanchez Moreno" -ForegroundColor Green
    Start-Sleep -Milliseconds 300
} catch { Write-Host "  ✗ Error" -ForegroundColor Yellow }

Write-Host "`n=== CREANDO ITEMS DE INVENTARIO ===" -ForegroundColor Cyan

# Item 1
try {
    $i1 = '{"name":"Guantes Latex Caja x100","description":"Guantes desechables latex talla M","sku":"GLT-M-100","initialQuantity":50,"minimumQuantity":10,"maximumQuantity":200,"reorderPoint":15,"unitPrice":8.50,"supplier":"DentalSupply SA","location":"Estante A1"}' 
    $ri1 = Invoke-RestMethod -Uri "$baseUrl/odontologia/kardex/items" -Method Post -Body $i1 -Headers $headers
    $script:itemIds += $ri1.id
    Write-Host "  ✓ Guantes Latex" -ForegroundColor Green
    Start-Sleep -Milliseconds 300
} catch { Write-Host "  ✗ Error" -ForegroundColor Yellow }

# Item 2
try {
    $i2 = '{"name":"Anestesia Lidocaina 2%","description":"Anestesia local con epinefrina","sku":"LIDO-2-50","initialQuantity":30,"minimumQuantity":5,"maximumQuantity":100,"reorderPoint":10,"unitPrice":2.75,"supplier":"Pharma Dental","location":"Refrigerador 1","expirationDate":"2026-12-31"}' 
    $ri2 = Invoke-RestMethod -Uri "$baseUrl/odontologia/kardex/items" -Method Post -Body $i2 -Headers $headers
    $script:itemIds += $ri2.id
    Write-Host "  ✓ Anestesia Lidocaina" -ForegroundColor Green
    Start-Sleep -Milliseconds 300
} catch { Write-Host "  ✗ Error" -ForegroundColor Yellow }

# Item 3
try {
    $i3 = '{"name":"Resina Compuesta A2","description":"Resina fotopolimerizable tono A2","sku":"RES-A2-4G","initialQuantity":20,"minimumQuantity":3,"maximumQuantity":50,"reorderPoint":5,"unitPrice":45.00,"supplier":"3M Dental","location":"Estante B2","batch":"LOT2024-A2"}' 
    $ri3 = Invoke-RestMethod -Uri "$baseUrl/odontologia/kardex/items" -Method Post -Body $i3 -Headers $headers
    $script:itemIds += $ri3.id
    Write-Host "  ✓ Resina Compuesta A2" -ForegroundColor Green
    Start-Sleep -Milliseconds 300
} catch { Write-Host "  ✗ Error" -ForegroundColor Yellow }

# Item 4
try {
    $i4 = '{"name":"Agujas Dentales 27G","description":"Agujas cortas anestesia dental","sku":"AGU-27G-100","initialQuantity":100,"minimumQuantity":20,"maximumQuantity":500,"reorderPoint":30,"unitPrice":0.35,"supplier":"DentalSupply SA","location":"Estante A2"}' 
    $ri4 = Invoke-RestMethod -Uri "$baseUrl/odontologia/kardex/items" -Method Post -Body $i4 -Headers $headers
    $script:itemIds += $ri4.id
    Write-Host "  ✓ Agujas Dentales 27G" -ForegroundColor Green
    Start-Sleep -Milliseconds 300
} catch { Write-Host "  ✗ Error" -ForegroundColor Yellow }

# Item 5
try {
    $i5 = '{"name":"Algodon en Rollos","description":"Rollos algodon esteril","sku":"ALG-ROL-500","initialQuantity":80,"minimumQuantity":15,"maximumQuantity":200,"reorderPoint":25,"unitPrice":5.20,"supplier":"Dental Imports","location":"Estante C1"}' 
    $ri5 = Invoke-RestMethod -Uri "$baseUrl/odontologia/kardex/items" -Method Post -Body $i5 -Headers $headers
    $script:itemIds += $ri5.id
    Write-Host "  ✓ Algodon en Rollos" -ForegroundColor Green
    Start-Sleep -Milliseconds 300
} catch { Write-Host "  ✗ Error" -ForegroundColor Yellow }

# Item 6
try {
    $i6 = '{"name":"Cepillos Dentales Adulto","description":"Cepillos cerdas suaves","sku":"CEP-ADU-12","initialQuantity":60,"minimumQuantity":10,"maximumQuantity":150,"reorderPoint":20,"unitPrice":1.50,"supplier":"Colgate Ecuador","location":"Estante D1"}' 
    $ri6 = Invoke-RestMethod -Uri "$baseUrl/odontologia/kardex/items" -Method Post -Body $i6 -Headers $headers
    $script:itemIds += $ri6.id
    Write-Host "  ✓ Cepillos Dentales" -ForegroundColor Green
    Start-Sleep -Milliseconds 300
} catch { Write-Host "  ✗ Error" -ForegroundColor Yellow }

# Item 7
try {
    $i7 = '{"name":"Hilo Dental Menta","description":"Hilo dental sabor menta","sku":"HIL-MEN-50","initialQuantity":45,"minimumQuantity":8,"maximumQuantity":100,"reorderPoint":15,"unitPrice":2.00,"supplier":"Oral-B Ecuador","location":"Estante D2"}' 
    $ri7 = Invoke-RestMethod -Uri "$baseUrl/odontologia/kardex/items" -Method Post -Body $i7 -Headers $headers
    $script:itemIds += $ri7.id
    Write-Host "  ✓ Hilo Dental Menta" -ForegroundColor Green
    Start-Sleep -Milliseconds 300
} catch { Write-Host "  ✗ Error" -ForegroundColor Yellow }

# Item 8
try {
    $i8 = '{"name":"Mascarillas Quirurgicas","description":"Mascarillas desechables 3 capas","sku":"MAS-3C-50","initialQuantity":200,"minimumQuantity":30,"maximumQuantity":1000,"reorderPoint":50,"unitPrice":0.25,"supplier":"MedSupply","location":"Estante A3"}' 
    $ri8 = Invoke-RestMethod -Uri "$baseUrl/odontologia/kardex/items" -Method Post -Body $i8 -Headers $headers
    $script:itemIds += $ri8.id
    Write-Host "  ✓ Mascarillas Quirurgicas" -ForegroundColor Green
    Start-Sleep -Milliseconds 300
} catch { Write-Host "  ✗ Error" -ForegroundColor Yellow }

Write-Host "`n=== CREANDO MOVIMIENTOS DE INVENTARIO ===" -ForegroundColor Cyan

if ($script:itemIds.Count -gt 0) {
    $fecha15 = (Get-Date).AddDays(-15).ToString("yyyy-MM-dd")
    $fecha10 = (Get-Date).AddDays(-10).ToString("yyyy-MM-dd")
    $fecha5 = (Get-Date).AddDays(-5).ToString("yyyy-MM-dd")
    
    # Entradas hace 15 días
    for ($i = 0; $i -lt [Math]::Min(3, $script:itemIds.Count); $i++) {
        try {
            $entryJson = "{`"inventoryItemId`":`"$($script:itemIds[$i])`",`"quantity`":25,`"unitPrice`":10.50,`"movementDate`":`"$fecha15`",`"reference`":`"Compra quincenal`",`"notes`":`"Reabastecimiento programado`"}"
            Invoke-RestMethod -Uri "$baseUrl/odontologia/kardex/entry" -Method Post -Body $entryJson -Headers $headers | Out-Null
            Write-Host "  ✓ Entrada hace 15 días" -ForegroundColor Green
            Start-Sleep -Milliseconds 200
        } catch { Write-Host "  ✗ Error entrada" -ForegroundColor Yellow }
    }
    
    # Salidas hace 10 días
    for ($i = 0; $i -lt [Math]::Min(4, $script:itemIds.Count); $i++) {
        try {
            $exitJson = "{`"inventoryItemId`":`"$($script:itemIds[$i])`",`"quantity`":5,`"unitPrice`":10.00,`"movementDate`":`"$fecha10`",`"reference`":`"Uso clinico`",`"notes`":`"Consumo de procedimientos`"}"
            Invoke-RestMethod -Uri "$baseUrl/odontologia/kardex/exit" -Method Post -Body $exitJson -Headers $headers | Out-Null
            Write-Host "  ✓ Salida hace 10 días" -ForegroundColor Green
            Start-Sleep -Milliseconds 200
        } catch { Write-Host "  ✗ Error salida" -ForegroundColor Yellow }
    }
    
    # Entradas hace 5 días
    for ($i = 3; $i -lt [Math]::Min(6, $script:itemIds.Count); $i++) {
        try {
            $entryJson = "{`"inventoryItemId`":`"$($script:itemIds[$i])`",`"quantity`":35,`"unitPrice`":12.00,`"movementDate`":`"$fecha5`",`"reference`":`"Compra semanal`",`"notes`":`"Reabastecimiento`"}"
            Invoke-RestMethod -Uri "$baseUrl/odontologia/kardex/entry" -Method Post -Body $entryJson -Headers $headers | Out-Null
            Write-Host "  ✓ Entrada hace 5 días" -ForegroundColor Green
            Start-Sleep -Milliseconds 200
        } catch { Write-Host "  ✗ Error entrada" -ForegroundColor Yellow }
    }
}

Write-Host "`n=== CREANDO HISTORIAS CLÍNICAS ===" -ForegroundColor Cyan

if ($script:patientIds.Count -gt 0) {
    $fecha20 = (Get-Date).AddDays(-20).ToString("yyyy-MM-dd")
    $fecha12 = (Get-Date).AddDays(-12).ToString("yyyy-MM-dd")
    $fecha8 = (Get-Date).AddDays(-8).ToString("yyyy-MM-dd")
    $fecha3 = (Get-Date).AddDays(-3).ToString("yyyy-MM-dd")
    
    # Historia 1
    try {
        $hc1 = "{`"patientId`":`"$($script:patientIds[0])`",`"data`":{`"motivo`":`"Limpieza dental y revision general`",`"anamnesis`":`"Paciente refiere sensibilidad dental leve`",`"examenClinico`":`"Presencia de calculo dental moderado`",`"diagnostico`":`"Gingivitis cronica leve`",`"tratamiento`":`"Profilaxis dental Aplicacion de fluor`",`"fecha`":`"$fecha20`"}}"
        Invoke-RestMethod -Uri "$baseUrl/clinical-histories" -Method Post -Body $hc1 -Headers $headers | Out-Null
        Write-Host "  ✓ HC: Limpieza dental" -ForegroundColor Green
        Start-Sleep -Milliseconds 300
    } catch { Write-Host "  ✗ Error HC1" -ForegroundColor Yellow }
    
    # Historia 2
    if ($script:patientIds.Count -gt 1) {
        try {
            $hc2 = "{`"patientId`":`"$($script:patientIds[1])`",`"data`":{`"motivo`":`"Dolor en muela del juicio`",`"anamnesis`":`"Dolor intermitente hace 3 dias`",`"examenClinico`":`"Tercer molar parcialmente erupcionado`",`"diagnostico`":`"Pericoronitis aguda`",`"tratamiento`":`"Antibiotico y analgesico`",`"fecha`":`"$fecha12`"}}"
            Invoke-RestMethod -Uri "$baseUrl/clinical-histories" -Method Post -Body $hc2 -Headers $headers | Out-Null
            Write-Host "  ✓ HC: Dolor muela juicio" -ForegroundColor Green
            Start-Sleep -Milliseconds 300
        } catch { Write-Host "  ✗ Error HC2" -ForegroundColor Yellow }
    }
    
    # Historia 3
    if ($script:patientIds.Count -gt 2) {
        try {
            $hc3 = "{`"patientId`":`"$($script:patientIds[2])`",`"data`":{`"motivo`":`"Caries dental`",`"anamnesis`":`"Paciente diabetica`",`"examenClinico`":`"Caries profunda en pieza 16`",`"diagnostico`":`"Caries dental profunda`",`"tratamiento`":`"Restauracion con resina`",`"fecha`":`"$fecha8`"}}"
            Invoke-RestMethod -Uri "$baseUrl/clinical-histories" -Method Post -Body $hc3 -Headers $headers | Out-Null
            Write-Host "  ✓ HC: Caries dental" -ForegroundColor Green
            Start-Sleep -Milliseconds 300
        } catch { Write-Host "  ✗ Error HC3" -ForegroundColor Yellow }
    }
    
    # Historia 4
    if ($script:patientIds.Count -gt 3) {
        try {
            $hc4 = "{`"patientId`":`"$($script:patientIds[3])`",`"data`":{`"motivo`":`"Control de ortodoncia`",`"anamnesis`":`"Paciente en tratamiento de ortodoncia`",`"examenClinico`":`"Brackets en buen estado`",`"diagnostico`":`"Malocusion Clase II en tratamiento`",`"tratamiento`":`"Cambio de ligaduras Activacion de arco`",`"fecha`":`"$fecha3`"}}"
            Invoke-RestMethod -Uri "$baseUrl/clinical-histories" -Method Post -Body $hc4 -Headers $headers | Out-Null
            Write-Host "  ✓ HC: Control ortodoncia" -ForegroundColor Green
            Start-Sleep -Milliseconds 300
        } catch { Write-Host "  ✗ Error HC4" -ForegroundColor Yellow }
    }
}

Write-Host "`n=== CREANDO GASTOS ===" -ForegroundColor Cyan

$gastosConfig = @(
    @{f=(Get-Date).AddDays(-25).ToString("yyyy-MM-dd");cat="Servicios Basicos";amt=145.50;desc="Luz electrica Enero 2026";pm="Transferencia"},
    @{f=(Get-Date).AddDays(-25).ToString("yyyy-MM-dd");cat="Servicios Basicos";amt=38.20;desc="Agua potable Enero 2026";pm="Transferencia"},
    @{f=(Get-Date).AddDays(-22).ToString("yyyy-MM-dd");cat="Servicios Basicos";amt=65.00;desc="Internet Enero 2026";pm="Debito"},
    @{f=(Get-Date).AddDays(-20).ToString("yyyy-MM-dd");cat="Salarios";amt=800.00;desc="Salario asistente dental";pm="Transferencia"},
    @{f=(Get-Date).AddDays(-18).ToString("yyyy-MM-dd");cat="Mantenimiento";amt=220.00;desc="Mantenimiento equipo rayos X";pm="Efectivo"},
    @{f=(Get-Date).AddDays(-15).ToString("yyyy-MM-dd");cat="Insumos";amt=350.00;desc="Compra materiales dentales";pm="Transferencia"},
    @{f=(Get-Date).AddDays(-10).ToString("yyyy-MM-dd");cat="Servicios Profesionales";amt=180.00;desc="Honorarios contador";pm="Transferencia"},
    @{f=(Get-Date).AddDays(-7).ToString("yyyy-MM-dd");cat="Publicidad";amt=120.00;desc="Publicidad redes sociales";pm="Tarjeta de Credito"},
    @{f=(Get-Date).AddDays(-5).ToString("yyyy-MM-dd");cat="Insumos";amt=95.50;desc="Material limpieza";pm="Efectivo"},
    @{f=(Get-Date).AddDays(-2).ToString("yyyy-MM-dd");cat="Servicios Basicos";amt=152.30;desc="Luz electrica Febrero 2026";pm="Transferencia"}
)

foreach ($g in $gastosConfig) {
    try {
        $gastoJson = "{`"category`":`"$($g.cat)`",`"amount`":$($g.amt),`"description`":`"$($g.desc)`",`"date`":`"$($g.f)`",`"paymentMethod`":`"$($g.pm)`"}"
        Invoke-RestMethod -Uri "$baseUrl/odontologia/gastos" -Method Post -Body $gastoJson -Headers $headers | Out-Null
        Write-Host "  ✓ $($g.desc)" -ForegroundColor Green
        Start-Sleep -Milliseconds 200
    } catch { Write-Host "  ✗ Error gasto" -ForegroundColor Yellow }
}

Write-Host "`n════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✓ DATOS DE PRUEBA CREADOS EXITOSAMENTE" -ForegroundColor Green
Write-Host "════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Resumen:" -ForegroundColor White
Write-Host "  • Pacientes: $($script:patientIds.Count)" -ForegroundColor Yellow
Write-Host "  • Items inventario: $($script:itemIds.Count)" -ForegroundColor Yellow
Write-Host "  • Movimientos inventario: ~20" -ForegroundColor Yellow
Write-Host "  • Historias clínicas: 4" -ForegroundColor Yellow
Write-Host "  • Gastos: $($gastosConfig.Count)" -ForegroundColor Yellow
Write-Host ""
Write-Host "Ahora actualiza el dashboard para ver los datos reales." -ForegroundColor Cyan
Write-Host ""
