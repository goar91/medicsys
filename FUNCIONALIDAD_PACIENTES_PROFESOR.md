# Funcionalidad de Registro de Pacientes para Profesores

## Fecha de implementación
5 de febrero de 2026

## Objetivo
Agregar funcionalidad completa de gestión de pacientes académicos al rol de Profesor, permitiendo registrar, visualizar, editar y eliminar pacientes desde el dashboard.

## Cambios Implementados

### 1. Backend (MEDICSYS.Api)

#### Modelo de Datos
**Archivo**: `MEDICSYS.Api/Models/Academico/AcademicPatient.cs`
- Modelo completo de paciente académico con campos:
  - Información personal: FirstName, LastName, IdNumber (cédula), DateOfBirth, Gender
  - Contacto: Phone, Email, Address
  - Información médica: BloodType, Allergies, MedicalConditions
  - Emergencia: EmergencyContact, EmergencyPhone
  - Relaciones: CreatedByProfessorId (FK a ApplicationUser)

#### Base de Datos
**Archivo**: `MEDICSYS.Api/Data/AcademicDbContext.cs`
- Agregado `DbSet<AcademicPatient> AcademicPatients`
- Configuración de entidad con índice único en IdNumber (cédula)
- Relación con tabla de usuarios (profesores)

#### Migración
- **Migración**: `20260205005702_AddAcademicPatients`
- **Comando ejecutado**: `dotnet ef migrations add AddAcademicPatients --context AcademicDbContext`
- **Estado**: ✅ Aplicada exitosamente con `dotnet ef database update --context AcademicDbContext`

#### API Controller
**Archivo**: `MEDICSYS.Api/Controllers/Academico/AcademicPatientsController.cs`
- Endpoints implementados:
  - `GET /api/academic/patients` - Lista todos los pacientes (con búsqueda opcional)
  - `GET /api/academic/patients/{id}` - Obtiene un paciente por ID
  - `POST /api/academic/patients` - Crea un nuevo paciente
  - `PUT /api/academic/patients/{id}` - Actualiza un paciente existente
  - `DELETE /api/academic/patients/{id}` - Elimina un paciente
- Autorización: Solo profesores (`[Authorize(Roles = Roles.Professor)]`)
- Validaciones: Previene duplicados de cédula (IdNumber)

### 2. Frontend (MEDICSYS.Web)

#### Servicio Angular
**Archivo**: `MEDICSYS.Web/src/app/core/academic.service.ts`
- Interface TypeScript `AcademicPatient` con todos los campos del modelo
- Métodos de servicio:
  - `getPatients(search?: string)` - Lista pacientes con búsqueda
  - `getPatientById(id: string)` - Obtiene paciente por ID
  - `createPatient(data)` - Crea nuevo paciente
  - `updatePatient(id, data)` - Actualiza paciente
  - `deletePatient(id)` - Elimina paciente

#### Dashboard del Profesor
**Archivos**: 
- `MEDICSYS.Web/src/app/pages/professor-dashboard/professor-dashboard.ts`
- `MEDICSYS.Web/src/app/pages/professor-dashboard/professor-dashboard.html`

**Nuevas características**:
- Signal `patients` para lista reactiva de pacientes
- Signal `showPatients` para alternar entre vista de historias y pacientes
- Métrica actualizada: "Pacientes" muestra conteo de pacientes registrados
- Botón "Ver Pacientes" / "Ver Historias" para cambiar de vista
- Vista de pacientes con:
  - Botón "Registrar Paciente" (navega a formulario)
  - Lista de pacientes con información: nombre, cédula, teléfono, email, tipo de sangre
  - Acciones por paciente: Ver, Editar, Eliminar
  - Mensaje cuando no hay pacientes registrados
- Método `togglePatients()` - Alterna vista de historias/pacientes
- Método `deletePatient(id)` - Elimina paciente con confirmación
- Carga automática de pacientes al inicializar (método `load()` actualizado)

#### Formulario de Pacientes
**Archivos**:
- `MEDICSYS.Web/src/app/pages/professor-patients-form/professor-patients-form.ts`
- `MEDICSYS.Web/src/app/pages/professor-patients-form/professor-patients-form.html`
- `MEDICSYS.Web/src/app/pages/professor-patients-form/professor-patients-form.scss`

**Características**:
- Formulario reactivo con validaciones completas
- Modo creación y edición (detecta automáticamente por parámetro de ruta)
- Secciones organizadas:
  1. **Información Personal**: Nombres, apellidos, cédula, fecha nacimiento, género, tipo sangre
  2. **Información de Contacto**: Teléfono, email, dirección
  3. **Información Médica**: Alergias, condiciones médicas
  4. **Contacto de Emergencia**: Nombre y teléfono

**Validaciones**:
- Campos requeridos: FirstName, LastName, IdNumber, DateOfBirth, Gender, Phone, EmergencyContact, EmergencyPhone
- Cédula: Exactamente 10 dígitos
- Teléfonos: Exactamente 10 dígitos
- Email: Formato válido de email (opcional)
- Mensajes de error descriptivos en tiempo real

#### Rutas
**Archivo**: `MEDICSYS.Web/src/app/app.routes.ts`
- `professor/dashboard` - Dashboard principal del profesor
- `professor/patients/new` - Formulario para crear nuevo paciente
- `professor/patients/:id/edit` - Formulario para editar paciente existente
- Protección con `authGuard` y `roleGuard` (solo rol "Profesor")

### 3. Actualización de Datos del Dashboard

#### Sistema Reactivo con Signals
El dashboard utiliza **Angular Signals** para actualizaciones automáticas:
- `patients()` signal se actualiza al:
  - Cargar el dashboard (ngOnInit)
  - Hacer clic en "Refrescar"
  - Eliminar un paciente (filtra localmente)
  - Crear/editar paciente (redirige a dashboard que recarga)

#### Métricas Computadas
- Las métricas se recalculan automáticamente cuando cambian los signals subyacentes
- Métrica "Pacientes" muestra: `this.patients().length`
- Actualización en tiempo real sin necesidad de refrescar manualmente

## Flujo de Uso

### Registrar Nuevo Paciente
1. Profesor inicia sesión con `profesor@medicsys.com` / `Profesor123!`
2. En dashboard, clic en botón "Ver Pacientes"
3. Clic en "Registrar Paciente"
4. Completar formulario con datos del paciente
5. Clic en "Registrar"
6. Sistema valida cédula única y guarda
7. Redirecciona a dashboard con paciente visible en la lista

### Ver/Editar Paciente
1. En vista de pacientes, clic en "Ver" o "Editar"
2. Formulario pre-cargado con datos del paciente
3. Modificar campos necesarios
4. Clic en "Actualizar"
5. Dashboard se actualiza con cambios

### Eliminar Paciente
1. En lista de pacientes, clic en "Eliminar"
2. Confirmar acción
3. Paciente eliminado y lista actualizada automáticamente

## Seguridad
- **Autorización**: Solo usuarios con rol "Profesor" pueden gestionar pacientes
- **Validación Backend**: Previene duplicados de cédula
- **Guards Frontend**: roleGuard protege rutas de pacientes
- **Confirmación**: Eliminación requiere confirmación del usuario

## Estado de Funcionamiento
✅ Backend compilado sin errores
✅ Base de datos migrada exitosamente
✅ Frontend compilado (1 warning menor de RouterLink sin uso)
✅ Servicios corriendo:
   - Backend: http://localhost:5000
   - Frontend: http://localhost:4200

## Archivos Creados
1. `MEDICSYS.Api/Models/Academico/AcademicPatient.cs`
2. `MEDICSYS.Api/Controllers/Academico/AcademicPatientsController.cs`
3. `MEDICSYS.Api/Migrations/xxxxxxxx_AddAcademicPatients.cs`
4. `MEDICSYS.Web/src/app/pages/professor-patients-form/professor-patients-form.ts`
5. `MEDICSYS.Web/src/app/pages/professor-patients-form/professor-patients-form.html`
6. `MEDICSYS.Web/src/app/pages/professor-patients-form/professor-patients-form.scss`

## Archivos Modificados
1. `MEDICSYS.Api/Data/AcademicDbContext.cs`
2. `MEDICSYS.Web/src/app/core/academic.service.ts`
3. `MEDICSYS.Web/src/app/pages/professor-dashboard/professor-dashboard.ts`
4. `MEDICSYS.Web/src/app/pages/professor-dashboard/professor-dashboard.html`
5. `MEDICSYS.Web/src/app/app.routes.ts`

## Próximos Pasos Sugeridos
- [ ] Agregar página de detalle de paciente (vista completa read-only)
- [ ] Implementar búsqueda/filtrado en lista de pacientes
- [ ] Agregar exportación de lista de pacientes a Excel/PDF
- [ ] Vincular pacientes con historias clínicas
- [ ] Agregar paginación si la lista de pacientes crece

## Notas Técnicas
- Se utiliza arquitectura de tres bases de datos separadas (medicsys, medicsys_academico, medicsys_odontologia)
- Pacientes académicos solo existen en `medicsys_academico`
- El ID del profesor que crea el paciente se guarda en `CreatedByProfessorId`
- Las fechas se manejan como strings ISO 8601 en el frontend
- Los teléfonos y cédulas usan validación de patrón regex para 10 dígitos exactos
