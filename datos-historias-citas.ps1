# Carga de historias clÃ­nicas y citas
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
Write-Host "   MEDICSYS - HISTORIAS Y CITAS" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Login odontologo
$loginOdon = @{ email = "odontologo@medicsys.com"; password = "Odontologo123!" }
$resOdon = Invoke-Api -Method Post -Uri "$apiUrl/auth/login" -Body $loginOdon
$headersOdon = @{ Authorization = "Bearer $($resOdon.token)" }

# Login estudiante (fallback a odontologo)
$headersHist = $headersOdon
try {
    $loginEst = @{ email = "estudiante1@medicsys.com"; password = "Estudiante123!" }
    $resEst = Invoke-Api -Method Post -Uri "$apiUrl/auth/login" -Body $loginEst
    $headersHist = @{ Authorization = "Bearer $($resEst.token)" }
} catch { }

$patients = @()
try { $patients = Invoke-Api -Method Get -Uri "$apiUrl/patients" -Headers $headersOdon } catch { }

$nombres = @("Juan","Maria","Carlos","Ana","Jose","Laura","Pedro","Sofia","Luis","Carmen","Miguel","Isabel","Antonio","Rosa","Francisco","Elena","Manuel","Patricia","David","Lucia")
$apellidos = @("Garcia","Rodriguez","Martinez","Lopez","Gonzalez","Perez","Sanchez","Torres","Flores","Rivera","Ortiz","Mendoza")
$servicios = @("Limpieza dental","Endodoncia","Extraccion","Resina","Control general","Ortodoncia","Radiografia")

Write-Host "[1/2] Creando 50 historias clÃ­nicas..." -ForegroundColor Yellow
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
Write-Host "   OK - Historias creadas" -ForegroundColor Green

Write-Host "[2/2] Creando 100 citas..." -ForegroundColor Yellow
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
Write-Host "   OK - Citas creadas" -ForegroundColor Green

Write-Host "`n================================================" -ForegroundColor Cyan
Write-Host "   CARGA COMPLETADA" -ForegroundColor Green
Write-Host "   - 50 historias clÃ­nicas" -ForegroundColor Green
Write-Host "   - 100 citas" -ForegroundColor Green
Write-Host "================================================`n" -ForegroundColor Cyan
