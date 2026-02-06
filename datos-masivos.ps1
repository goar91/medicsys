# Carga masiva de datos para MEDICSYS
$ErrorActionPreference = "Continue"
$apiUrl = "http://localhost:5154/api"
$delayMs = 500

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Uri,
        [hashtable]$Headers,
        $Body = $null
    )

    $attempt = 0
    while ($true) {
        try {
            if ($null -eq $Body) {
                return Invoke-RestMethod -Uri $Uri -Method $Method -Headers $Headers -TimeoutSec 20
            }

            $json = $Body | ConvertTo-Json -Depth 10
            return Invoke-RestMethod -Uri $Uri -Method $Method -Headers $Headers -ContentType "application/json" -Body $json -TimeoutSec 20
        }
        catch {
            $status = $_.Exception.Response.StatusCode.value__
            if (($status -eq 429 -or $status -eq 500 -or $status -eq 503) -and $attempt -lt 3) {
                Start-Sleep -Seconds 3
                $attempt++
                continue
            }
            throw
        }
    }
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "   MEDICSYS - CARGA MASIVA DE DATOS" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Login odontologo
Write-Host "[1/6] Autenticando odontologo..." -ForegroundColor Yellow
$loginOdon = @{
    email = "odontologo@medicsys.com"
    password = "Odontologo123!"
}
$resOdon = Invoke-Api -Method Post -Uri "$apiUrl/auth/login" -Body $loginOdon
$headersOdon = @{ Authorization = "Bearer $($resOdon.token)" }
Write-Host "   OK - Odontologo" -ForegroundColor Green

# Login estudiante (fallback a odontologo si falla)
Write-Host "[2/6] Autenticando estudiante..." -ForegroundColor Yellow
$headersHist = $headersOdon
try {
    $loginEst = @{
        email = "estudiante1@medicsys.com"
        password = "Estudiante123!"
    }
    $resEst = Invoke-Api -Method Post -Uri "$apiUrl/auth/login" -Body $loginEst
    $headersHist = @{ Authorization = "Bearer $($resEst.token)" }
    Write-Host "   OK - Estudiante" -ForegroundColor Green
}
catch {
    Write-Host "   Aviso: No se pudo autenticar estudiante, se usarÃ¡ odontologo para historias." -ForegroundColor Yellow
}

# Base de datos de pacientes (para enlazar historias)
$patients = @()
try {
    $patients = Invoke-Api -Method Get -Uri "$apiUrl/patients" -Headers $headersOdon
} catch { $patients = @() }

$nombres = @("Juan","Maria","Carlos","Ana","Jose","Laura","Pedro","Sofia","Luis","Carmen","Miguel","Isabel","Antonio","Rosa","Francisco","Elena","Manuel","Patricia","David","Lucia")
$apellidos = @("Garcia","Rodriguez","Martinez","Lopez","Gonzalez","Perez","Sanchez","Torres","Flores","Rivera","Ortiz","Mendoza")
$servicios = @("Limpieza dental","Endodoncia","Extraccion","Resina","Control general","Ortodoncia","Radiografia")
$categorias = @("Insumos","Servicios","Mantenimiento","Publicidad","Equipos","Papeleria","Transporte")
$proveedores = @("DentalPro","Insumos S.A.","ClinicaPlus","Suministros Med","Proveedor Local")
$metodos = @("Cash","Transfer","Card")
$itemsFactura = @("Consulta","Limpieza","Resina","Endodoncia","Radiografia","Control","Ortodoncia")

Write-Host "`n[3/6] Creando 100 artÃ­culos de inventario..." -ForegroundColor Yellow
for ($i = 1; $i -le 100; $i++) {
    $name = "Item $i - " + ($itemsFactura | Get-Random)
    $exp = (Get-Date).AddDays((Get-Random -Min -180 -Max 365))
    $inv = @{
        name = $name
        description = "Articulo odontologico $i"
        sku = "SKU-$([string]::Format('{0:0000}', $i))"
        quantity = Get-Random -Min 5 -Max 200
        minimumQuantity = Get-Random -Min 2 -Max 15
        unitPrice = [math]::Round((Get-Random -Min 5 -Max 150) + ((Get-Random -Min 0 -Max 100) / 100), 2)
        supplier = $proveedores | Get-Random
        expirationDate = $exp.ToString("yyyy-MM-dd")
    }
    Invoke-Api -Method Post -Uri "$apiUrl/odontologia/inventory" -Headers $headersOdon -Body $inv | Out-Null
    Start-Sleep -Milliseconds $delayMs
}
Write-Host "   OK - Inventario creado" -ForegroundColor Green

Write-Host "`n[4/6] Creando 50 gastos..." -ForegroundColor Yellow
for ($i = 1; $i -le 50; $i++) {
    $date = (Get-Date).AddDays((Get-Random -Min -180 -Max 1)).ToUniversalTime().ToString("o")
    $gasto = @{
        description = "Gasto $i - " + ($categorias | Get-Random)
        amount = [math]::Round((Get-Random -Min 20 -Max 500) + ((Get-Random -Min 0 -Max 100) / 100), 2)
        expenseDate = $date
        category = $categorias | Get-Random
        paymentMethod = $metodos | Get-Random
        invoiceNumber = "G-$([string]::Format('{0:0000}', $i))"
        supplier = $proveedores | Get-Random
        notes = "Registro generado automaticamente"
    }
    Invoke-Api -Method Post -Uri "$apiUrl/odontologia/gastos" -Headers $headersOdon -Body $gasto | Out-Null
    Start-Sleep -Milliseconds $delayMs
}
Write-Host "   OK - Gastos creados" -ForegroundColor Green

Write-Host "`n[5/6] Creando 50 facturas..." -ForegroundColor Yellow
for ($i = 1; $i -le 50; $i++) {
    $nom = $nombres | Get-Random
    $ape = $apellidos | Get-Random
    $cid = "17" + (Get-Random -Min 10000000 -Max 99999999)
    $items = @()
    $itemCount = Get-Random -Min 1 -Max 4
    for ($j = 1; $j -le $itemCount; $j++) {
        $items += @{
            description = ($itemsFactura | Get-Random)
            quantity = Get-Random -Min 1 -Max 4
            unitPrice = [math]::Round((Get-Random -Min 20 -Max 200) + ((Get-Random -Min 0 -Max 100) / 100), 2)
            discountPercent = Get-Random -Min 0 -Max 10
        }
    }

    $fact = @{
        customerIdentificationType = "05"
        customerIdentification = $cid
        customerName = "$nom $ape"
        customerAddress = "Av. Principal #$i"
        customerPhone = "09" + (Get-Random -Min 10000000 -Max 99999999)
        customerEmail = "$($nom.ToLower()).$($ape.ToLower())$i@medicsys.demo"
        observations = "Factura generada automaticamente"
        paymentMethod = "Cash"
        items = $items
    }
    Invoke-Api -Method Post -Uri "$apiUrl/invoices" -Headers $headersOdon -Body $fact | Out-Null
    Start-Sleep -Milliseconds $delayMs
}
Write-Host "   OK - Facturas creadas" -ForegroundColor Green

Write-Host "`n[6/6] Creando 50 historias clÃ­nicas y 100 citas..." -ForegroundColor Yellow

for ($i = 1; $i -le 50; $i++) {
    $nom = $nombres | Get-Random
    $ape = $apellidos | Get-Random
    $dob = (Get-Date).AddYears(-(Get-Random -Min 18 -Max 70)).ToString("yyyy-MM-dd")
    $patientId = $null
    if ($patients.Count -gt 0) {
        $patientId = ($patients | Get-Random).id
    }

    $data = @{
        personal = @{
            firstName = $nom
            lastName = $ape
            idNumber = "17" + (Get-Random -Min 10000000 -Max 99999999)
            dateOfBirth = $dob
            gender = ("M","F") | Get-Random
            phone = "09" + (Get-Random -Min 10000000 -Max 99999999)
        }
        consulta = @{
            motivo = $servicios | Get-Random
            diagnostico = ("Caries leve","Gingivitis","Control","Dolor dental","Sensibilidad") | Get-Random
            tratamiento = ("Limpieza","Resina","Analgesicos","Endodoncia","Seguimiento") | Get-Random
        }
        observaciones = "Historia generada automaticamente"
        fecha = (Get-Date).AddDays((Get-Random -Min -120 -Max 1)).ToString("yyyy-MM-dd")
    }

    $hist = @{
        patientId = $patientId
        data = $data
    }

    Invoke-Api -Method Post -Uri "$apiUrl/clinical-histories" -Headers $headersHist -Body $hist | Out-Null
    Start-Sleep -Milliseconds $delayMs
}

for ($i = 1; $i -le 100; $i++) {
    $start = (Get-Date).AddDays((Get-Random -Min -30 -Max 60)).Date.AddHours((Get-Random -Min 8 -Max 18))
    $duration = (30,45,60) | Get-Random
    $end = $start.AddMinutes($duration)
    $appt = @{
        patientName = ($nombres | Get-Random) + " " + ($apellidos | Get-Random)
        reason = $servicios | Get-Random
        startAt = $start.ToUniversalTime().ToString("o")
        endAt = $end.ToUniversalTime().ToString("o")
        notes = "Cita generada automaticamente"
        status = ("Pending","Confirmed","Completed") | Get-Random
    }
    Invoke-Api -Method Post -Uri "$apiUrl/agenda/appointments" -Headers $headersOdon -Body $appt | Out-Null
    Start-Sleep -Milliseconds $delayMs
}

Write-Host "`n================================================" -ForegroundColor Cyan
Write-Host "   CARGA COMPLETADA" -ForegroundColor Green
Write-Host "   - 100 inventario" -ForegroundColor Green
Write-Host "   - 50 gastos" -ForegroundColor Green
Write-Host "   - 50 facturas" -ForegroundColor Green
Write-Host "   - 50 historias clÃ­nicas" -ForegroundColor Green
Write-Host "   - 100 citas" -ForegroundColor Green
Write-Host "================================================`n" -ForegroundColor Cyan
