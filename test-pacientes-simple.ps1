# Script de prueba simple para pacientes
$baseUrl = "http://localhost:5000/api"

Write-Host "=== PRUEBA GESTION DE PACIENTES ===" -ForegroundColor Cyan

# 1. Login
Write-Host "`n1. Login..." -ForegroundColor Yellow
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body (@{
    email = "profesor@medicsys.com"
    password = "Profesor123!"
} | ConvertTo-Json) -ContentType "application/json"

Write-Host "OK - Token obtenido" -ForegroundColor Green

$headers = @{
    "Authorization" = "Bearer $($loginResponse.token)"
    "Content-Type" = "application/json"
}

# 2. Listar pacientes
Write-Host "`n2. Listar pacientes..." -ForegroundColor Yellow
try {
    $pacientes = Invoke-RestMethod -Uri "$baseUrl/academic/patients" -Method Get -Headers $headers
    Write-Host "OK - Total: $($pacientes.Count) pacientes" -ForegroundColor Green
} catch {
    Write-Host "ERROR: $_" -ForegroundColor Red
}

# 3. Crear paciente
Write-Host "`n3. Crear paciente..." -ForegroundColor Yellow
$nuevoPaciente = @{
    firstName = "Juan"
    lastName = "Perez Test"
    idNumber = "1234567890"
    dateOfBirth = "1990-05-15"
    gender = "Masculino"
    phone = "0987654321"
    email = "juan.test@mail.com"
    address = "Av Test 123"
    bloodType = "O+"
    allergies = "Ninguna"
    medicalConditions = "Ninguna"
    emergencyContact = "Maria Perez"
    emergencyPhone = "0998765432"
}

try {
    $pacienteCreado = Invoke-RestMethod -Uri "$baseUrl/academic/patients" -Method Post -Body ($nuevoPaciente | ConvertTo-Json) -Headers $headers
    Write-Host "OK - Paciente creado: $($pacienteCreado.fullName)" -ForegroundColor Green
    Write-Host "   ID: $($pacienteCreado.id)" -ForegroundColor Gray
    $idCreado = $pacienteCreado.id
} catch {
    Write-Host "ERROR: $_" -ForegroundColor Red
    Write-Host "Detalle: $($_.Exception.Message)" -ForegroundColor Red
    $idCreado = $null
}

# 4. Obtener por ID
if ($idCreado) {
    Write-Host "`n4. Obtener paciente por ID..." -ForegroundColor Yellow
    try {
        $paciente = Invoke-RestMethod -Uri "$baseUrl/academic/patients/$idCreado" -Method Get -Headers $headers
        Write-Host "OK - Paciente: $($paciente.fullName)" -ForegroundColor Green
    } catch {
        Write-Host "ERROR: $_" -ForegroundColor Red
    }

    # 5. Actualizar
    Write-Host "`n5. Actualizar paciente..." -ForegroundColor Yellow
    $nuevoPaciente.allergies = "Penicilina"
    try {
        $actualizado = Invoke-RestMethod -Uri "$baseUrl/academic/patients/$idCreado" -Method Put -Body ($nuevoPaciente | ConvertTo-Json) -Headers $headers
        Write-Host "OK - Alergias actualizadas: $($actualizado.allergies)" -ForegroundColor Green
    } catch {
        Write-Host "ERROR: $_" -ForegroundColor Red
    }

    # 6. Eliminar
    Write-Host "`n6. Eliminar paciente..." -ForegroundColor Yellow
    try {
        Invoke-RestMethod -Uri "$baseUrl/academic/patients/$idCreado" -Method Delete -Headers $headers | Out-Null
        Write-Host "OK - Paciente eliminado" -ForegroundColor Green
    } catch {
        Write-Host "ERROR: $_" -ForegroundColor Red
    }
}

Write-Host "`n=== FIN PRUEBAS ===" -ForegroundColor Cyan
