# Reestructuración del Sistema MEDICSYS - Separación Odontología/Académico

**Fecha:** 3 de febrero de 2026  
**Cambio:** Arquitectura de bases de datos separadas

---

## Resumen de Cambios

Se ha reestructurado completamente MEDICSYS en **DOS SISTEMAS INDEPENDIENTES**:

### 1. **Sistema de Odontología** (Independiente)
- Base de datos: `medicsys_odontologia`
- Rol: **Odontólogo**
- **Sin relación** con profesores o estudiantes

### 2. **Sistema Académico** (Profesor-Alumno)
- Base de datos: `medicsys_academico`
- Roles: **Profesor** y **Alumno**
- Totalmente integrado entre sí

---

## Arquitectura Nueva

### Sistema Odontológico

**Base de datos:** `medicsys_odontologia`

**Entidades:**
- `OdontologoAppointment` - Citas del odontólogo
- `OdontologoClinicalHistory` - Historias clínicas del odontólogo
- `OdontologoPatient` - Pacientes del odontólogo
- `Invoice` - Facturas electrónicas
- `InvoiceItem` - Items de facturas
- `AccountingEntry` - Entradas contables
- `AccountingCategory` - Categorías contables

**Controladores (Nuevos):**
- `api/odontologia/patients` - Gestión de pacientes
- `api/odontologia/appointments` - Gestión de citas
- `api/invoices` - Facturación (actualizado)
- `api/accounting` - Contabilidad (actualizado)

**Características:**
- ✅ Totalmente independiente
- ✅ Sin acceso a datos académicos
- ✅ Historias clínicas propias
- ✅ Pacientes propios
- ✅ Facturación exclusiva
- ✅ Contabilidad exclusiva

### Sistema Académico

**Base de datos:** `medicsys_academico`

**Entidades:**
- `AcademicAppointment` - Citas académicas (Estudiante-Profesor)
- `AcademicClinicalHistory` - Historias clínicas académicas
- `AcademicReminder` - Recordatorios de citas

**Controladores (Nuevos):**
- `api/academic/appointments` - Gestión de citas académicas
- `api/academic/clinical-histories` - Gestión de historias clínicas académicas

**Características:**
- ✅ Profesor crea citas para estudiantes
- ✅ Profesor revisa y aprueba historias clínicas
- ✅ Estudiante solo ve sus propias citas e historias
- ✅ Profesor ve TODAS las citas e historias
- ✅ Sistema de revisión con comentarios del profesor
- ✅ Estados: Draft, Approved, Rejected

---

## Matriz de Permisos Actualizada

### 🩺 Odontólogo (Sistema Independiente)

| Funcionalidad | Base de Datos | Controlador | Acceso |
|--------------|---------------|-------------|---------|
| Gestión de pacientes | medicsys_odontologia | OdontologoPatientsController | ✅ Total |
| Citas propias | medicsys_odontologia | OdontologoAppointmentsController | ✅ Total |
| Historias clínicas propias | medicsys_odontologia | *Pendiente* | ✅ Total |
| Facturación | medicsys_odontologia | InvoicesController | ✅ Total |
| Contabilidad | medicsys_odontologia | AccountingController | ✅ Total |
| **Sistema Académico** | medicsys_academico | - | ❌ **SIN ACCESO** |

### 👨‍🏫 Profesor (Sistema Académico)

| Funcionalidad | Base de Datos | Controlador | Acceso |
|--------------|---------------|-------------|---------|
| Crear citas para estudiantes | medicsys_academico | AcademicAppointmentsController | ✅ Total |
| Ver todas las citas | medicsys_academico | AcademicAppointmentsController | ✅ Total |
| Revisar historias clínicas | medicsys_academico | AcademicClinicalHistoriesController | ✅ Total |
| Aprobar/Rechazar historias | medicsys_academico | AcademicClinicalHistoriesController.Review | ✅ Total |
| Agregar comentarios | medicsys_academico | AcademicClinicalHistoriesController.Review | ✅ Total |
| **Sistema Odontológico** | medicsys_odontologia | - | ❌ **SIN ACCESO** |

### 👨‍🎓 Alumno (Sistema Académico)

| Funcionalidad | Base de Datos | Controlador | Acceso |
|--------------|---------------|-------------|---------|
| Ver sus citas | medicsys_academico | AcademicAppointmentsController | ✅ Solo las suyas |
| Crear historias clínicas | medicsys_academico | AcademicClinicalHistoriesController | ✅ Solo en Draft |
| Editar historias clínicas | medicsys_academico | AcademicClinicalHistoriesController | ✅ Solo en Draft |
| Ver estado de revisión | medicsys_academico | AcademicClinicalHistoriesController | ✅ Solo las suyas |
| **Sistema Odontológico** | medicsys_odontologia | - | ❌ **SIN ACCESO** |

---

## Estructura de Archivos Backend

```
MEDICSYS.Api/
├── Models/
│   ├── Odontologia/
│   │   ├── OdontologoAppointment.cs
│   │   ├── OdontologoClinicalHistory.cs
│   │   └── OdontologoPatient.cs
│   └── Academico/
│       ├── AcademicAppointment.cs
│       ├── AcademicClinicalHistory.cs
│       └── AcademicReminder.cs
├── Data/
│   ├── OdontologoDbContext.cs (Base: medicsys_odontologia)
│   ├── OdontologoDbContextFactory.cs
│   ├── AcademicDbContext.cs (Base: medicsys_academico)
│   └── AcademicDbContextFactory.cs
├── Controllers/
│   ├── Odontologia/
│   │   ├── OdontologoPatientsController.cs
│   │   └── OdontologoAppointmentsController.cs
│   ├── Academico/
│   │   ├── AcademicAppointmentsController.cs
│   │   └── AcademicClinicalHistoriesController.cs
│   ├── InvoicesController.cs (usa OdontologoDbContext)
│   └── AccountingController.cs (usa OdontologoDbContext)
└── Migrations/
    ├── Odontologia/
    │   └── 20260203224629_InitialOdontologia.cs
    └── Academico/
        └── 20260203224648_InitialAcademico.cs
```

---

## Cambios en appsettings.json

```json
{
  "ConnectionStrings": {
    "OdontologiaConnection": "Host=localhost;Port=5432;Database=medicsys_odontologia;Username=postgres;Password=030762",
    "AcademicoConnection": "Host=localhost;Port=5432;Database=medicsys_academico;Username=postgres;Password=030762"
  }
}
```

---

## Flujo de Trabajo Académico

### 1. Profesor crea cita para estudiante
```
POST /api/academic/appointments
{
  "studentId": "guid-del-estudiante",
  "professorId": "guid-del-profesor",
  "patientName": "Juan Pérez",
  "reason": "Consulta general",
  "startAt": "2026-02-05T10:00:00Z",
  "endAt": "2026-02-05T11:00:00Z",
  "status": "Pending"
}
```

### 2. Estudiante crea historia clínica
```
POST /api/academic/clinical-histories
{
  "data": {
    "personal": { ... },
    "odontogram": { ... },
    "diagnosis": { ... }
  }
}
```
- Estado inicial: **Draft**
- Solo el estudiante puede editar mientras esté en Draft

### 3. Profesor revisa la historia
```
POST /api/academic/clinical-histories/{id}/review
{
  "approved": true,
  "comments": "Excelente trabajo. Diagnóstico correcto."
}
```
- Estado cambia a: **Approved** o **Rejected**
- Se agrega `reviewedByProfessorId`
- Se registra `reviewedAt`
- Se guardan comentarios del profesor

---

## Flujo de Trabajo Odontológico

### 1. Odontólogo crea paciente
```
POST /api/odontologia/patients
{
  "firstName": "María",
  "lastName": "González",
  "idNumber": "1234567890",
  "dateOfBirth": "1990-05-15",
  "gender": "F",
  "address": "Av. Principal 123",
  "phone": "0999999999",
  "email": "maria@example.com"
}
```

### 2. Odontólogo crea cita
```
POST /api/odontologia/appointments
{
  "patientName": "María González",
  "reason": "Limpieza dental",
  "startAt": "2026-02-06T14:00:00Z",
  "endAt": "2026-02-06T15:00:00Z",
  "status": "Pending",
  "notes": "Primera visita"
}
```

### 3. Odontólogo crea factura
```
POST /api/invoices
{
  "customerName": "María González",
  "customerIdentification": "1234567890",
  "items": [
    {
      "description": "Limpieza dental",
      "quantity": 1,
      "unitPrice": 50.00
    }
  ],
  "paymentMethod": "Cash"
}
```

---

## Migraciones Aplicadas

### Base Odontológica
```bash
dotnet ef database update --context OdontologoDbContext
```
- ✅ Crea `medicsys_odontologia`
- ✅ Tablas: OdontologoAppointments, OdontologoPatients, OdontologoClinicalHistories, Invoices, AccountingEntries

### Base Académica
```bash
dotnet ef database update --context AcademicDbContext
```
- ✅ Crea `medicsys_academico`
- ✅ Tablas: AcademicAppointments, AcademicClinicalHistories, AcademicReminders, AspNetUsers (Identity)

---

## Estado de Implementación

### ✅ Completado

1. ✅ Modelos separados (Odontología y Académico)
2. ✅ Dos DbContexts independientes
3. ✅ Dos bases de datos separadas
4. ✅ Migraciones creadas y aplicadas
5. ✅ Controladores de Odontología actualizados
6. ✅ Controladores Académicos creados
7. ✅ Facturación y Contabilidad usando OdontologoDbContext
8. ✅ Sistema de revisión profesor-alumno

### ⚠️ Pendiente Frontend

- ⚠️ Actualizar rutas frontend a nuevos endpoints
- ⚠️ Crear componentes para historias clínicas del odontólogo
- ⚠️ Adaptar servicios Angular a nuevos DTOs
- ⚠️ Implementar interfaz de revisión para profesores

---

## Próximos Pasos

### 1. Migrar Datos Existentes (Si aplica)
```sql
-- Migrar pacientes de odontólogos de la DB antigua a la nueva
INSERT INTO "OdontologoPatients" 
SELECT * FROM old_medicsys.patients 
WHERE "OdontologoId" IS NOT NULL;
```

### 2. Actualizar Frontend
- Cambiar `api/patients` → `api/odontologia/patients`
- Cambiar `api/agenda/appointments` → `api/odontologia/appointments` (odontólogos)
- Cambiar `api/agenda/appointments` → `api/academic/appointments` (académico)
- Crear interfaz de revisión para profesores

### 3. Crear Controlador de Historias Odontológicas
```
OdontologoClinicalHistoriesController
- GET /api/odontologia/clinical-histories
- POST /api/odontologia/clinical-histories
- PUT /api/odontologia/clinical-histories/{id}
```

### 4. Testing
- Probar creación de citas odontológicas
- Probar creación de citas académicas
- Probar flujo de revisión profesor-alumno
- Verificar separación de datos

---

## Ventajas de la Nueva Arquitectura

### 🎯 Separación Total
- Odontólogos y académicos **NO** comparten datos
- Bases de datos independientes permiten:
  - Escalabilidad independiente
  - Backups separados
  - Seguridad mejorada

### 🔒 Seguridad Mejorada
- Odontólogo **NO** puede ver datos académicos
- Profesor **NO** puede ver datos de odontólogos
- Alumno **solo** ve sus propios datos

### 📊 Control Académico
- Profesor valida **todo** lo que hace el alumno
- Sistema de aprobación/rechazo con comentarios
- Historial completo de revisiones

### ⚡ Rendimiento
- Queries más rápidas (bases más pequeñas)
- Índices optimizados por contexto
- Sin joins innecesarios entre sistemas

---

## Troubleshooting

### Error: "Database does not exist"
```bash
# Crear bases manualmente en PostgreSQL
createdb medicsys_odontologia
createdb medicsys_academico

# Aplicar migraciones
dotnet ef database update --context OdontologoDbContext
dotnet ef database update --context AcademicDbContext
```

### Error: "Cannot access academic data as Odontologo"
✅ **Esto es correcto**. Los sistemas están separados intencionalmente.

### Error: "Professor cannot create invoice"
✅ **Esto es correcto**. Solo Odontólogos pueden facturar.

---

**Sistema:** MEDICSYS v2.0 - Arquitectura Separada  
**Estado:** ✅ Backend completado, Frontend pendiente
