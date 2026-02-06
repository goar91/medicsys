# Script de prueba de login para estudiantes
Write-Host "Iniciando prueba de login..." -ForegroundColor Cyan

# Esperar a que el backend esté listo
Write-Host "Esperando a que el backend este listo..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Intentar login con estudiante1
Write-Host "`nProbando login con estudiante1@medicsys.com..." -ForegroundColor Yellow

$body = @{
    email = "estudiante1@medicsys.com"
    password = "Estudiante123!"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5154/api/auth/login" -Method POST -ContentType "application/json" -Body $body
    
    Write-Host "`n============================================" -ForegroundColor Green
    Write-Host "LOGIN EXITOSO!" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "Usuario: $($response.user.fullName)" -ForegroundColor Cyan
    Write-Host "Email: $($response.user.email)" -ForegroundColor Cyan
    Write-Host "Rol: $($response.user.role)" -ForegroundColor Yellow
    Write-Host "ID: $($response.user.id)" -ForegroundColor Gray
    Write-Host "Token (primeros 50 chars): $($response.token.Substring(0,50))..." -ForegroundColor Gray
    Write-Host "`nEl login funciona correctamente!" -ForegroundColor Green
    
} catch {
    Write-Host "`n============================================" -ForegroundColor Red
    Write-Host "ERROR DE LOGIN" -ForegroundColor Red
    Write-Host "============================================" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "Mensaje: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        Write-Host "Detalles: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    
    Write-Host "`nPosibles causas:" -ForegroundColor Yellow
    Write-Host "1. El backend no esta corriendo" -ForegroundColor Gray
    Write-Host "2. El usuario no existe en la base de datos" -ForegroundColor Gray
    Write-Host "3. La contrasena es incorrecta" -ForegroundColor Gray
}

# Intentar con profesor como verificación adicional
Write-Host "`n`nProbando login con profesor@medicsys.com..." -ForegroundColor Yellow

$body2 = @{
    email = "profesor@medicsys.com"
    password = "Profesor123!"
} | ConvertTo-Json

try {
    $response2 = Invoke-RestMethod -Uri "http://localhost:5154/api/auth/login" -Method POST -ContentType "application/json" -Body $body2
    
    Write-Host "`n============================================" -ForegroundColor Green
    Write-Host "LOGIN PROFESOR EXITOSO!" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "Usuario: $($response2.user.fullName)" -ForegroundColor Cyan
    Write-Host "Email: $($response2.user.email)" -ForegroundColor Cyan
    Write-Host "Rol: $($response2.user.role)" -ForegroundColor Yellow
    
} catch {
    Write-Host "ERROR - Profesor tampoco puede hacer login" -ForegroundColor Red
    Write-Host "Detalles: $($_.ErrorDetails.Message)" -ForegroundColor Red
}

Write-Host "`n" -ForegroundColor White
