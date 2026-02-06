# Test eliminar paciente
$baseUrl = "http://localhost:5000/api"

Write-Host "=== TEST ELIMINAR PACIENTE ===" -ForegroundColor Cyan

# Login
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body (@{
    email = "profesor@medicsys.com"
    password = "Profesor123!"
} | ConvertTo-Json) -ContentType "application/json"

$headers = @{
    "Authorization" = "Bearer $($loginResponse.token)"
    "Content-Type" = "application/json"
}

# Listar pacientes
Write-Host "`n1. Listando pacientes..." -ForegroundColor Yellow
$pacientes = Invoke-RestMethod -Uri "$baseUrl/academic/patients" -Method Get -Headers $headers
Write-Host "Total: $($pacientes.Count) pacientes" -ForegroundColor Green

if ($pacientes.Count -gt 0) {
    $primerPaciente = $pacientes[0]
    Write-Host "Primer paciente: $($primerPaciente.fullName) (ID: $($primerPaciente.id))" -ForegroundColor Gray
    
    # Eliminar
    Write-Host "`n2. Eliminando paciente..." -ForegroundColor Yellow
    try {
        Invoke-WebRequest -Uri "$baseUrl/academic/patients/$($primerPaciente.id)" -Method Delete -Headers $headers -UseBasicParsing | Out-Null
        Write-Host "OK - Paciente eliminado" -ForegroundColor Green
    } catch {
        Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            Write-Host $reader.ReadToEnd() -ForegroundColor Red
        }
    }
    
    # Verificar
    Write-Host "`n3. Verificando eliminación..." -ForegroundColor Yellow
    $pacientesDespues = Invoke-RestMethod -Uri "$baseUrl/academic/patients" -Method Get -Headers $headers
    Write-Host "Total después: $($pacientesDespues.Count) pacientes" -ForegroundColor Green
    
    if ($pacientesDespues.Count -eq ($pacientes.Count - 1)) {
        Write-Host "CORRECTO - El paciente fue eliminado" -ForegroundColor Green
    } else {
        Write-Host "ERROR - El paciente NO fue eliminado" -ForegroundColor Red
    }
} else {
    Write-Host "No hay pacientes para eliminar" -ForegroundColor Yellow
}

Write-Host "`n=== FIN ===" -ForegroundColor Cyan
