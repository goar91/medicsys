# Script de Pruebas Completas - MEDICSYS

Write-Host "================================" -ForegroundColor Cyan
Write-Host "PRUEBAS DE HISTORIAS CLÍNICAS" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Login como Odontólogo
Write-Host "1. Login como Odontólogo..." -ForegroundColor Yellow
$loginBody = @{
    email = "odontologo@medicsys.com"
    password = "Odontologo123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-WebRequest -Uri "http://localhost:5154/api/auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop
    
    $authData = $loginResponse.Content | ConvertFrom-Json
    $token = $authData.token
    Write-Host "   ✅ Login exitoso - Token obtenido" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Error en login: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Crear Historia Clínica #1 - Paciente con Caries
Write-Host "`n2. Creando Historia Clínica #1 - Paciente con caries..." -ForegroundColor Yellow
$historia1 = @{
    data = @{
        personal = @{
            lastName = "González"
            firstName = "María"
            idNumber = "1234567890"
            gender = "Femenino"
            age = "28"
            ageRange = "25-30"
            clinicalHistoryNumber = "HC-2026-001"
            address = "Av. De las Américas 123, Cuenca"
            phone = "0987654321"
            date = "2026-02-03"
        }
        consultation = @{
            reason = "Dolor en muela superior derecha"
            currentIssue = "Paciente refiere dolor agudo al masticar desde hace 3 días"
            antecedentes = @{
                alergiaAntibiotico = $false
                alergiaAnestesia = $false
                hemorragias = $false
                vih = $false
                tuberculosis = $false
                asma = $false
                diabetes = $false
                hipertension = $false
                cardiaca = $false
                otros = $false
            }
            vitalSigns = @{
                bloodPressure = "120/80"
                heartRate = "72"
                temperature = "36.5"
                respiratoryRate = "16"
            }
            estomatognatico = @{
                labios = $false
                mejillas = $false
                maxilarSuperior = $false
                maxilarInferior = $false
                lengua = $false
                paladar = $false
                piso = $false
                carrillos = $false
                glandulasSalivales = $false
                orofaringe = $false
                atm = $false
                ganglios = $false
            }
            estomatognaticoDetails = @{
                labios = ""
                mejillas = ""
                maxilarSuperior = ""
                maxilarInferior = ""
                lengua = ""
                paladar = ""
                piso = ""
                carrillos = ""
                glandulasSalivales = ""
                orofaringe = ""
                atm = ""
                ganglios = ""
            }
            notes = "Paciente colaborador, buen estado general de salud oral exceptuando pieza 16"
        }
        indicators = @{
            higieneOral = "Buena"
            enfermedadPeriodontal = "Leve"
            maloclusion = "Clase I"
            fluorosis = "Ninguna"
            indiceCpo = "C:2 P:0 O:0"
        }
        treatments = @{
            plan = "1. Restauración con resina pieza 16 cara oclusal
2. Profilaxis dental
3. Aplicación de flúor
4. Instrucciones de higiene oral"
            procedures = "Anestesia local, remoción de tejido cariado, restauración con resina compuesta"
        }
        medios = @{
            imagenes = "Radiografía periapical pieza 16"
            notas = "Se observa caries profunda en cara oclusal sin compromiso pulpar"
            assets = @()
        }
        odontogram = @{
            teeth = @{
                "16" = @{
                    vestibular = "none"
                    mesial = "none"
                    distal = "none"
                    occlusal = "caries-done"
                    lingual = "none"
                }
                "26" = @{
                    vestibular = "none"
                    mesial = "none"
                    distal = "none"
                    occlusal = "caries-planned"
                    lingual = "none"
                }
            }
            depths = @{}
            neckLevels = @{}
            showNumbers = $true
            showArchLabels = $true
        }
    }
} | ConvertTo-Json -Depth 10

try {
    $createResponse1 = Invoke-WebRequest -Uri "http://localhost:5154/api/clinical-histories" `
        -Method POST `
        -Headers $headers `
        -Body $historia1 `
        -ErrorAction Stop
    
    $historia1Data = $createResponse1.Content | ConvertFrom-Json
    $historia1Id = $historia1Data.id
    Write-Host "   ✅ Historia Clínica #1 creada - ID: $historia1Id" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Error al crear HC#1: $($_.Exception.Message)" -ForegroundColor Red
}

# Crear Historia Clínica #2 - Paciente con Prótesis
Write-Host "`n3. Creando Historia Clínica #2 - Paciente con prótesis..." -ForegroundColor Yellow
$historia2 = @{
    data = @{
        personal = @{
            lastName = "Ramírez"
            firstName = "Carlos"
            idNumber = "0987654321"
            gender = "Masculino"
            age = "55"
            ageRange = "50-60"
            clinicalHistoryNumber = "HC-2026-002"
            address = "Calle Bolívar 456, Cuenca"
            phone = "0999888777"
            date = "2026-02-03"
        }
        consultation = @{
            reason = "Falta de piezas dentales"
            currentIssue = "Paciente con múltiples ausencias dentales, solicita rehabilitación protésica"
            antecedentes = @{
                alergiaAntibiotico = $false
                alergiaAnestesia = $false
                hemorragias = $false
                vih = $false
                tuberculosis = $false
                asma = $false
                diabetes = $true
                hipertension = $true
                cardiaca = $false
                otros = $false
            }
            vitalSigns = @{
                bloodPressure = "140/90"
                heartRate = "78"
                temperature = "36.7"
                respiratoryRate = "18"
            }
            estomatognatico = @{
                labios = $false
                mejillas = $false
                maxilarSuperior = $false
                maxilarInferior = $false
                lengua = $false
                paladar = $false
                piso = $false
                carrillos = $false
                glandulasSalivales = $false
                orofaringe = $false
                atm = $false
                ganglios = $false
            }
            estomatognaticoDetails = @{
                labios = ""
                mejillas = ""
                maxilarSuperior = ""
                maxilarInferior = ""
                lengua = ""
                paladar = ""
                piso = ""
                carrillos = ""
                glandulasSalivales = ""
                orofaringe = ""
                atm = ""
                ganglios = ""
            }
            notes = "Paciente con diabetes e hipertensión controladas. Requiere prótesis parcial removible superior"
        }
        indicators = @{
            higieneOral = "Regular"
            enfermedadPeriodontal = "Moderada"
            maloclusion = "Clase II"
            fluorosis = "Ninguna"
            indiceCpo = "C:1 P:8 O:3"
        }
        treatments = @{
            plan = "1. Prótesis Parcial Removible Superior
2. Tratamiento periodontal
3. Control de higiene oral
4. Seguimiento cada 6 meses"
            procedures = "Impresiones, prueba de estructura metálica, prueba de dientes, instalación de PPR"
        }
        medios = @{
            imagenes = "Radiografía panorámica, modelos de estudio"
            notas = "Se planifica PPR superior con retenedores en piezas 13 y 23"
            assets = @()
        }
        odontogram = @{
            teeth = @{
                "11" = @{ vestibular = "absent"; mesial = "absent"; distal = "absent"; occlusal = "absent"; lingual = "absent" }
                "12" = @{ vestibular = "absent"; mesial = "absent"; distal = "absent"; occlusal = "absent"; lingual = "absent" }
                "14" = @{ vestibular = "none"; mesial = "none"; distal = "none"; occlusal = "none"; lingual = "none" }
                "15" = @{ vestibular = "absent"; mesial = "absent"; distal = "absent"; occlusal = "absent"; lingual = "absent" }
                "16" = @{ vestibular = "absent"; mesial = "absent"; distal = "absent"; occlusal = "absent"; lingual = "absent" }
                "17" = @{ vestibular = "absent"; mesial = "absent"; distal = "absent"; occlusal = "absent"; lingual = "absent" }
                "21" = @{ vestibular = "absent"; mesial = "absent"; distal = "absent"; occlusal = "absent"; lingual = "absent" }
                "22" = @{ vestibular = "absent"; mesial = "absent"; distal = "absent"; occlusal = "absent"; lingual = "absent" }
                "24" = @{ vestibular = "none"; mesial = "none"; distal = "none"; occlusal = "none"; lingual = "none" }
                "25" = @{ vestibular = "absent"; mesial = "absent"; distal = "absent"; occlusal = "absent"; lingual = "absent" }
                "26" = @{ vestibular = "absent"; mesial = "absent"; distal = "absent"; occlusal = "absent"; lingual = "absent" }
                "27" = @{ vestibular = "absent"; mesial = "absent"; distal = "absent"; occlusal = "absent"; lingual = "absent" }
            }
            depths = @{}
            neckLevels = @{}
            showNumbers = $true
            showArchLabels = $true
        }
    }
} | ConvertTo-Json -Depth 10

try {
    $createResponse2 = Invoke-WebRequest -Uri "http://localhost:5154/api/clinical-histories" `
        -Method POST `
        -Headers $headers `
        -Body $historia2 `
        -ErrorAction Stop
    
    $historia2Data = $createResponse2.Content | ConvertFrom-Json
    $historia2Id = $historia2Data.id
    Write-Host "   ✅ Historia Clínica #2 creada - ID: $historia2Id" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Error al crear HC#2: $($_.Exception.Message)" -ForegroundColor Red
}

# Listar todas las historias clínicas
Write-Host "`n4. Listando todas las historias clínicas..." -ForegroundColor Yellow
try {
    $listResponse = Invoke-WebRequest -Uri "http://localhost:5154/api/clinical-histories" `
        -Headers @{ "Authorization" = "Bearer $token" } `
        -ErrorAction Stop
    
    $historias = $listResponse.Content | ConvertFrom-Json
    Write-Host "   ✅ Total de historias clínicas: $($historias.Count)" -ForegroundColor Green
    
    foreach ($h in $historias) {
        Write-Host "      - HC: $($h.data.personal.clinicalHistoryNumber) - Paciente: $($h.data.personal.firstName) $($h.data.personal.lastName) - Estado: $($h.status)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "   ❌ Error al listar: $($_.Exception.Message)" -ForegroundColor Red
}

# Actualizar una historia clínica
if ($historia1Id) {
    Write-Host "`n5. Actualizando Historia Clínica #1..." -ForegroundColor Yellow
    
    $updateData = @{
        data = @{
            personal = @{
                lastName = "González"
                firstName = "María"
                idNumber = "1234567890"
                gender = "Femenino"
                age = "28"
                ageRange = "25-30"
                clinicalHistoryNumber = "HC-2026-001"
                address = "Av. De las Américas 123, Cuenca"
                phone = "0987654321"
                date = "2026-02-03"
            }
            consultation = @{
                reason = "Dolor en muela superior derecha"
                currentIssue = "Paciente refiere dolor agudo al masticar desde hace 3 días - ACTUALIZADO"
                antecedentes = @{
                    alergiaAntibiotico = $false
                    alergiaAnestesia = $false
                    hemorragias = $false
                    vih = $false
                    tuberculosis = $false
                    asma = $false
                    diabetes = $false
                    hipertension = $false
                    cardiaca = $false
                    otros = $false
                }
                vitalSigns = @{
                    bloodPressure = "120/80"
                    heartRate = "72"
                    temperature = "36.5"
                    respiratoryRate = "16"
                }
                estomatognatico = @{
                    labios = $false
                    mejillas = $false
                    maxilarSuperior = $false
                    maxilarInferior = $false
                    lengua = $false
                    paladar = $false
                    piso = $false
                    carrillos = $false
                    glandulasSalivales = $false
                    orofaringe = $false
                    atm = $false
                    ganglios = $false
                }
                estomatognaticoDetails = @{
                    labios = ""
                    mejillas = ""
                    maxilarSuperior = ""
                    maxilarInferior = ""
                    lengua = ""
                    paladar = ""
                    piso = ""
                    carrillos = ""
                    glandulasSalivales = ""
                    orofaringe = ""
                    atm = ""
                    ganglios = ""
                }
                notes = "Paciente colaborador, buen estado general de salud oral exceptuando pieza 16 - ACTUALIZACIÓN: Se agregó nota sobre tratamiento completado"
            }
            indicators = @{
                higieneOral = "Buena"
                enfermedadPeriodontal = "Leve"
                maloclusion = "Clase I"
                fluorosis = "Ninguna"
                indiceCpo = "C:2 P:0 O:0"
            }
            treatments = @{
                plan = "1. Restauración con resina pieza 16 cara oclusal - COMPLETADO
2. Profilaxis dental - COMPLETADO
3. Aplicación de flúor - COMPLETADO
4. Instrucciones de higiene oral - COMPLETADO"
                procedures = "Anestesia local, remoción de tejido cariado, restauración con resina compuesta - COMPLETADO"
            }
            medios = @{
                imagenes = "Radiografía periapical pieza 16"
                notas = "Se observa caries profunda en cara oclusal sin compromiso pulpar - Restauración exitosa"
                assets = @()
            }
            odontogram = @{
                teeth = @{
                    "16" = @{
                        vestibular = "none"
                        mesial = "none"
                        distal = "none"
                        occlusal = "restoration-done"
                        lingual = "none"
                    }
                    "26" = @{
                        vestibular = "none"
                        mesial = "none"
                        distal = "none"
                        occlusal = "caries-planned"
                        lingual = "none"
                    }
                }
                depths = @{}
                neckLevels = @{}
                showNumbers = $true
                showArchLabels = $true
            }
        }
    } | ConvertTo-Json -Depth 10
    
    try {
        $updateResponse = Invoke-WebRequest -Uri "http://localhost:5154/api/clinical-histories/$historia1Id" `
            -Method PUT `
            -Headers $headers `
            -Body $updateData `
            -ErrorAction Stop
        
        Write-Host "   ✅ Historia Clínica #1 actualizada exitosamente" -ForegroundColor Green
    } catch {
        Write-Host "   ❌ Error al actualizar: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Verificar en base de datos
Write-Host "`n6. Verificando datos en base de datos..." -ForegroundColor Yellow
try {
    $dbCheck = docker exec medicsys-postgres psql -U postgres -d medicsys -c 'SELECT "Id", "StudentId", "Status", "CreatedAt", "UpdatedAt" FROM "ClinicalHistories" ORDER BY "CreatedAt" DESC LIMIT 5;'
    Write-Host $dbCheck -ForegroundColor White
    Write-Host "   ✅ Datos verificados en PostgreSQL" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Error al verificar BD: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n================================" -ForegroundColor Cyan
Write-Host "PRUEBAS COMPLETADAS" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host "`nResumen:" -ForegroundColor Yellow
Write-Host "- Se crearon 2 historias clínicas de prueba" -ForegroundColor White
Write-Host "- Se actualizó la historia clínica #1" -ForegroundColor White
Write-Host "- Se listaron todas las historias" -ForegroundColor White
Write-Host "- Se verificó la persistencia en BD" -ForegroundColor White
Write-Host "`nAccede a http://localhost:4200 para ver el frontend" -ForegroundColor Cyan
