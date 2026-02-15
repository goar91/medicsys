# Test detallado con manejo de errores
$baseUrl = "http://localhost:5000/api"

Write-Host "=== TEST DETALLADO ===" -ForegroundColor Cyan

# Login
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body (@{
    email = "profesor@medicsys.com"
    password = "Profesor123!"
} | ConvertTo-Json) -ContentType "application/json"

Write-Host "Login OK" -ForegroundColor Green

$headers = @{
    "Authorization" = "Bearer $($loginResponse.token)"
    "Content-Type" = "application/json"
}

# Crear paciente con manejo de error detallado
Write-Host "`nCreando paciente..." -ForegroundColor Yellow
$nuevoPaciente = @{
    firstName = "Juan"
    lastName = "Perez Test"
    idNumber = "1234567890"
    dateOfBirth = "1990-05-15T00:00:00Z"
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

Write-Host "Datos a enviar:" -ForegroundColor Gray
$nuevoPaciente | ConvertTo-Json | Write-Host -ForegroundColor DarkGray

try {
    $response = Invoke-WebRequest -Uri "$baseUrl/academic/patients" -Method Post -Body ($nuevoPaciente | ConvertTo-Json) -Headers $headers -ErrorAction Stop
    Write-Host "OK - Status: $($response.StatusCode)" -ForegroundColor Green
    $pacienteCreado = $response.Content | ConvertFrom-Json
    Write-Host "Paciente creado: $($pacienteCreado.fullName)" -ForegroundColor Green
    Write-Host "ID: $($pacienteCreado.id)" -ForegroundColor Gray
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Respuesta del servidor:" -ForegroundColor Yellow
        Write-Host $responseBody -ForegroundColor Red
    }
}

Write-Host "`n=== FIN ===" -ForegroundColor Cyan
