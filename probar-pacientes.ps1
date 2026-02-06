# Script de prueba para Pacientes del Profesor
# Fecha: 4 de febrero de 2026

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "PRUEBA: Gestión de Pacientes - Profesor" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$baseUrl = "http://localhost:5000/api"
$token = ""

# 1. Login como profesor
Write-Host "1. Iniciando sesión como profesor..." -ForegroundColor Yellow
try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body (@{
        email = "profesor@medicsys.com"
        password = "Profesor123!"
    } | ConvertTo-Json) -ContentType "application/json"
    
    $token = $loginResponse.token
    Write-Host "   ✓ Login exitoso. Token obtenido." -ForegroundColor Green
    Write-Host "   Usuario: $($loginResponse.user.fullName)" -ForegroundColor Gray
    Write-Host "   Rol: $($loginResponse.user.role)`n" -ForegroundColor Gray
} catch {
    Write-Host "   ✗ Error en login: $_" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# 2. Listar pacientes existentes
Write-Host "2. Listando pacientes existentes..." -ForegroundColor Yellow
try {
    $pacientes = Invoke-RestMethod -Uri "$baseUrl/academic/patients" -Method Get -Headers $headers
    Write-Host "   ✓ Total de pacientes: $($pacientes.Count)" -ForegroundColor Green
    if ($pacientes.Count -gt 0) {
        Write-Host "   Primeros pacientes:" -ForegroundColor Gray
        $pacientes | Select-Object -First 3 | ForEach-Object {
            Write-Host "   - $($_.fullName) (Cédula: $($_.idNumber))" -ForegroundColor Gray
        }
    }
    Write-Host ""
} catch {
    Write-Host "   ✗ Error al listar pacientes: $_" -ForegroundColor Red
    Write-Host "   Detalle: $($_.Exception.Message)" -ForegroundColor Red
}

# 3. Crear un nuevo paciente
Write-Host "3. Creando nuevo paciente de prueba..." -ForegroundColor Yellow
$cedulaPrueba = "1234567890"
$nuevoPaciente = @{
    firstName = "Juan"
    lastName = "Pérez Prueba"
    idNumber = $cedulaPrueba
    dateOfBirth = "1990-05-15"
    gender = "Masculino"
    phone = "0987654321"
    email = "juan.perez@test.com"
    address = "Av. Prueba 123, Quito"
    bloodType = "O+"
    allergies = "Ninguna conocida"
    medicalConditions = "Ninguna"
    emergencyContact = "María Pérez"
    emergencyPhone = "0998765432"
}

try {
    $pacienteCreado = Invoke-RestMethod -Uri "$baseUrl/academic/patients" -Method Post -Body ($nuevoPaciente | ConvertTo-Json) -Headers $headers
    Write-Host "   ✓ Paciente creado exitosamente" -ForegroundColor Green
    Write-Host "   ID: $($pacienteCreado.id)" -ForegroundColor Gray
    Write-Host "   Nombre: $($pacienteCreado.fullName)" -ForegroundColor Gray
    Write-Host "   Cédula: $($pacienteCreado.idNumber)" -ForegroundColor Gray
    Write-Host "   Email: $($pacienteCreado.email)`n" -ForegroundColor Gray
    
    $idPacienteCreado = $pacienteCreado.id
} catch {
    Write-Host "   ✗ Error al crear paciente: $_" -ForegroundColor Red
    Write-Host "   Detalle: $($_.Exception.Message)" -ForegroundColor Red
    $idPacienteCreado = $null
}

# 4. Obtener paciente por ID
if ($idPacienteCreado) {
    Write-Host "4. Obteniendo paciente por ID..." -ForegroundColor Yellow
    try {
        $paciente = Invoke-RestMethod -Uri "$baseUrl/academic/patients/$idPacienteCreado" -Method Get -Headers $headers
        Write-Host "   ✓ Paciente obtenido:" -ForegroundColor Green
        Write-Host "   Nombre: $($paciente.fullName)" -ForegroundColor Gray
        Write-Host "   Fecha nacimiento: $($paciente.dateOfBirth)" -ForegroundColor Gray
        Write-Host "   Contacto emergencia: $($paciente.emergencyContact) - $($paciente.emergencyPhone)`n" -ForegroundColor Gray
    } catch {
        Write-Host "   ✗ Error al obtener paciente: $_" -ForegroundColor Red
    }

    # 5. Actualizar paciente
    Write-Host "5. Actualizando datos del paciente..." -ForegroundColor Yellow
    $pacienteActualizado = $nuevoPaciente.Clone()
    $pacienteActualizado.allergies = "Penicilina, Polen"
    $pacienteActualizado.medicalConditions = "Hipertensión controlada"
    
    try {
        $resultado = Invoke-RestMethod -Uri "$baseUrl/academic/patients/$idPacienteCreado" -Method Put -Body ($pacienteActualizado | ConvertTo-Json) -Headers $headers
        Write-Host "   ✓ Paciente actualizado exitosamente" -ForegroundColor Green
        Write-Host "   Alergias: $($resultado.allergies)" -ForegroundColor Gray
        Write-Host "   Condiciones: $($resultado.medicalConditions)`n" -ForegroundColor Gray
    } catch {
        Write-Host "   ✗ Error al actualizar paciente: $_" -ForegroundColor Red
        Write-Host "   Detalle: $($_.Exception.Message)" -ForegroundColor Red
    }

    # 6. Eliminar paciente
    Write-Host "6. Eliminando paciente de prueba..." -ForegroundColor Yellow
    try {
        Invoke-RestMethod -Uri "$baseUrl/academic/patients/$idPacienteCreado" -Method Delete -Headers $headers
        Write-Host "   ✓ Paciente eliminado exitosamente`n" -ForegroundColor Green
    } catch {
        Write-Host "   ✗ Error al eliminar paciente: $_" -ForegroundColor Red
        Write-Host "   Detalle: $($_.Exception.Message)`n" -ForegroundColor Red
    }

    # 7. Verificar eliminación
    Write-Host "7. Verificando eliminación..." -ForegroundColor Yellow
    try {
        $paciente = Invoke-RestMethod -Uri "$baseUrl/academic/patients/$idPacienteCreado" -Method Get -Headers $headers
        Write-Host "   ✗ ERROR: El paciente aún existe" -ForegroundColor Red
    } catch {
        if ($_.Exception.Response.StatusCode -eq 404) {
            Write-Host "   ✓ Confirmado: Paciente eliminado correctamente`n" -ForegroundColor Green
        } else {
            Write-Host "   ? Error inesperado: $_" -ForegroundColor Yellow
        }
    }
}

# 8. Probar validación de cédula duplicada
Write-Host "8. Probando validación de cédula duplicada..." -ForegroundColor Yellow
try {
    # Intentar crear el mismo paciente dos veces
    $pac1 = Invoke-RestMethod -Uri "$baseUrl/academic/patients" -Method Post -Body ($nuevoPaciente | ConvertTo-Json) -Headers $headers
    Write-Host "   Primera creación: ✓" -ForegroundColor Gray
    
    try {
        $pac2 = Invoke-RestMethod -Uri "$baseUrl/academic/patients" -Method Post -Body ($nuevoPaciente | ConvertTo-Json) -Headers $headers
        Write-Host "   ✗ ERROR: Permitió duplicado de cédula" -ForegroundColor Red
    } catch {
        Write-Host "   ✓ Validación correcta: No permite cédulas duplicadas" -ForegroundColor Green
    }
    
    # Limpiar
    Invoke-RestMethod -Uri "$baseUrl/academic/patients/$($pac1.id)" -Method Delete -Headers $headers | Out-Null
} catch {
    Write-Host "   ✗ Error en prueba: $_" -ForegroundColor Red
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "PRUEBAS COMPLETADAS" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan
