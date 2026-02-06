# Pruebas para Estudiantes - MEDICSYS

## Credenciales Corregidas

### Estudiante 1
- **Email:** estudiante1@medicsys.com
- **Password:** Estudiante123!
- **Nombre:** Estudiante 1
- **ID Universitario:** EST001

### Estudiante 2
- **Email:** estudiante2@medicsys.com
- **Password:** Estudiante123!
- **Nombre:** Estudiante 2
- **ID Universitario:** EST002

### Estudiante 3
- **Email:** estudiante3@medicsys.com
- **Password:** Estudiante123!
- **Nombre:** Estudiante 3
- **ID Universitario:** EST003

### Profesor
- **Email:** profesor@medicsys.com
- **Password:** Profesor123!
- **Nombre:** Dr. Fernando Sánchez

## Correcciones Realizadas

### 1. **Credenciales Actualizadas**
   - ✅ Archivo `Usuarios y claves.txt` actualizado con emails correctos
   - ✅ Contraseñas consistentes entre backend y documentación
   - ✅ `appsettings.json` corregido con las credenciales del profesor

### 2. **Lógica de Login Corregida**
   - ✅ La redirección ahora usa el rol real del backend (`Alumno`) en lugar del valor del formulario
   - ✅ El `roleGuard` verificará correctamente el rol `Alumno`
   - ✅ Eliminado uso del campo `userType` del formulario para redirección

### 3. **Rutas de Script Corregidas**
   - ✅ Script `iniciar-medicsys.ps1` actualizado con rutas correctas de `C:\MEDICSYS\MEDICSYS\`

## Funcionalidades del Estudiante

### Dashboard (`/student`)
- Ver métricas: borradores, en revisión, aprobadas, citas de hoy
- Lista de historias clínicas propias
- Lista de citas programadas
- Botón para crear nueva historia clínica

### Historias Clínicas
- **Crear** (`/student/histories/new`): Formulario completo con validaciones
- **Editar** (`/student/histories/:id`): Solo si está en estado Draft
- **Enviar para revisión**: Cambiar estado a Submitted
- **Ver estado**: Draft, Submitted, Approved, Rejected
- **Ver comentarios del profesor**: Cuando ha sido revisada

### Citas Académicas
- **Ver citas propias**: En agenda y dashboard
- **Crear citas**: Programar prácticas con pacientes
- **Ver detalles**: Información completa de cada cita

## Pasos para Probar

1. **Iniciar el Sistema**
   ```powershell
   cd C:\MEDICSYS\MEDICSYS
   .\iniciar-medicsys.ps1
   ```

2. **Iniciar Sesión como Estudiante**
   - Ir a http://localhost:4200
   - Seleccionar "Estudiante" en el tipo de usuario
   - Email: `estudiante1@medicsys.com`
   - Password: `Estudiante123!`

3. **Verificar Dashboard**
   - Debe redirigir a `/student`
   - Ver métricas de historias clínicas
   - Ver lista de citas

4. **Crear Historia Clínica**
   - Click en "Nueva Historia"
   - Llenar formulario
   - Guardar como borrador
   - Enviar para revisión

5. **Ver Agenda**
   - Click en "Agenda" en el menú
   - Ver citas programadas
   - Crear nueva cita

## Verificación de Base de Datos

Para verificar que los usuarios se crearon correctamente, ejecutar PostgreSQL:

```sql
-- Conectar a la base de datos académica
\c medicsys_academico

-- Ver todos los usuarios estudiantes
SELECT "Id", "Email", "FullName", "UniversityId" 
FROM "AspNetUsers" 
WHERE "Email" LIKE '%estudiante%';

-- Ver roles asignados
SELECT u."Email", r."Name" 
FROM "AspNetUsers" u
JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" LIKE '%estudiante%';
```

## Problemas Conocidos y Soluciones

### Problema 1: "No puedo entrar al rol de estudiantes"
**Causa:** Email incorrecto en documentación (`alumno1@medicsys.local` vs `estudiante1@medicsys.com`)
**Solución:** ✅ Credenciales actualizadas en todos los archivos

### Problema 2: Redirección incorrecta después del login
**Causa:** Uso del valor del formulario en lugar del rol del backend
**Solución:** ✅ Lógica de redirección actualizada para usar `response.user.role`

### Problema 3: Error "Falta la cadena en el terminador" en script
**Causa:** Rutas incorrectas en `iniciar-medicsys.ps1`
**Solución:** ✅ Rutas actualizadas a `C:\MEDICSYS\MEDICSYS\`

## Endpoints API para Estudiantes

### Autenticación
- `POST /api/auth/register-student` - Registro de nuevo estudiante
- `POST /api/auth/login` - Inicio de sesión
- `GET /api/auth/me` - Obtener perfil del usuario actual

### Historias Clínicas Académicas
- `GET /api/academic/clinical-histories` - Listar historias (filtradas automáticamente por studentId)
- `GET /api/academic/clinical-histories/{id}` - Ver detalle de una historia
- `POST /api/academic/clinical-histories` - Crear nueva historia
- `PUT /api/academic/clinical-histories/{id}` - Actualizar historia (solo Draft)
- `POST /api/academic/clinical-histories/{id}/submit` - Enviar para revisión
- `DELETE /api/academic/clinical-histories/{id}` - Eliminar historia

### Citas Académicas
- `GET /api/academic/appointments` - Listar citas (filtradas automáticamente por studentId)
- `POST /api/academic/appointments` - Crear nueva cita
- `PUT /api/academic/appointments/{id}` - Actualizar cita
- `DELETE /api/academic/appointments/{id}` - Eliminar cita

### Recordatorios
- `GET /api/academic/reminders` - Listar recordatorios
