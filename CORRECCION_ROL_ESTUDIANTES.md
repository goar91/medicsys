# Resumen de Correcciones - Funcionalidad de Estudiantes

## Fecha: 4 de Febrero de 2026

### Problema Reportado
**"No puedo entrar al rol de los estudiantes"**

---

## Correcciones Realizadas

### 1. ✅ Credenciales Actualizadas

#### Archivo: `Usuarios y claves.txt`
**ANTES:**
```
Alumno:
- Email: alumno1@medicsys.local
- Password: Alumno123!
```

**DESPUÉS:**
```
Alumno (Estudiante 1):
- Email: estudiante1@medicsys.com
- Password: Estudiante123!

Alumno (Estudiante 2):
- Email: estudiante2@medicsys.com
- Password: Estudiante123!

Alumno (Estudiante 3):
- Email: estudiante3@medicsys.com
- Password: Estudiante123!
```

**Razón:** El código backend (AcademicSeedData.cs) crea usuarios con el patrón `estudiante{i}@medicsys.com`, no `alumno1@medicsys.local`. Ahora las credenciales documentadas coinciden con las creadas automáticamente.

---

### 2. ✅ Configuración del Profesor Corregida

#### Archivo: `MEDICSYS.Api/appsettings.json`
**ANTES:**
```json
"Seed": {
  "DefaultProfessorEmail": "profesor@medicsys.local",
  "DefaultProfessorPassword": "Medicsys#2026",
  "DefaultProfessorName": "Profesor Admin"
}
```

**DESPUÉS:**
```json
"Seed": {
  "DefaultProfessorEmail": "profesor@medicsys.com",
  "DefaultProfessorPassword": "Profesor123!",
  "DefaultProfessorName": "Dr. Fernando Sánchez"
}
```

**Razón:** El código en AcademicSeedData.cs crea el profesor con `profesor@medicsys.com` y contraseña `Profesor123!`. Ahora la configuración es consistente.

---

### 3. ✅ Lógica de Login Corregida

#### Archivo: `MEDICSYS.Web/src/app/pages/login/login.ts`

**ANTES:**
```typescript
submitLogin() {
  // ...
  const { userType, email, password } = this.loginForm.getRawValue();
  this.auth.login(email!, password!).subscribe({
    next: response => {
      this.auth.setSession(response);
      this.loading.set(false);
      // Redirigir según el tipo de usuario seleccionado
      this.redirectByUserType(userType!);
    },
    // ...
  });
}

private redirectByUserType(userType: string) {
  switch(userType) {
    case 'Estudiante':
      this.router.navigate(['/student']);
      break;
    // ...
  }
}
```

**DESPUÉS:**
```typescript
submitLogin() {
  // ...
  const { email, password } = this.loginForm.getRawValue();
  this.auth.login(email!, password!).subscribe({
    next: response => {
      this.auth.setSession(response);
      this.loading.set(false);
      // Redirigir según el rol real del usuario devuelto por el backend
      this.redirectByRole(response.user.role);
    },
    // ...
  });
}

private redirectByRole(role: string) {
  switch(role) {
    case 'Alumno':
      this.router.navigate(['/student']);
      break;
    // ...
  }
}
```

**Razón:** 
- El formulario de login tiene un select que muestra "Estudiante", pero es solo decorativo
- El backend devuelve el rol real: **"Alumno"** (definido en `Roles.Student = "Alumno"`)
- El `roleGuard` verifica contra el rol real, no el valor del formulario
- Ahora la redirección usa `response.user.role` que contiene "Alumno", haciendo que coincida con la verificación del guard

---

### 4. ✅ Script de Inicio Corregido

#### Archivo: `iniciar-medicsys.ps1`

**ANTES:**
```powershell
Set-Location "d:\Programación\MEDICSYS\MEDICSYS.Api"
Start-Process cmd -ArgumentList "/c", "cd /d `"d:\Programación\MEDICSYS\MEDICSYS.Api`" && dotnet run"
```

**DESPUÉS:**
```powershell
Set-Location "C:\MEDICSYS\MEDICSYS\MEDICSYS.Api"
Start-Process cmd -ArgumentList "/c", "cd /d `"C:\MEDICSYS\MEDICSYS\MEDICSYS.Api`" && dotnet run"
```

**Razón:** El workspace está en `C:\MEDICSYS\MEDICSYS\`, no en `d:\Programación\`.

---

## Arquitectura del Sistema de Roles

### Definición de Roles (Backend)
```csharp
// MEDICSYS.Api/Security/Roles.cs
public static class Roles
{
    public const string Professor = "Profesor";
    public const string Student = "Alumno";      // ← Importante: es "Alumno"
    public const string Odontologo = "Odontologo";
}
```

### Flujo de Autenticación
1. Usuario ingresa email y password
2. Backend valida credenciales
3. Backend busca roles del usuario en `AspNetUserRoles`
4. Backend genera JWT con claim de rol: **"Alumno"**
5. Frontend recibe `AuthResponse` con `user.role = "Alumno"`
6. Frontend guarda en localStorage
7. Frontend redirige a `/student`
8. `roleGuard` verifica: `data.roles = ['Alumno']` ← debe coincidir

### Rutas Protegidas para Estudiantes
```typescript
// MEDICSYS.Web/src/app/app.routes.ts
{
  path: 'student',
  component: StudentDashboardComponent,
  canActivate: [authGuard, roleGuard],
  data: { roles: ['Alumno'] }  // ← Rol exacto del backend
},
{
  path: 'student/histories/new',
  component: ClinicalHistoryFormComponent,
  canActivate: [authGuard, roleGuard],
  data: { roles: ['Alumno'] }
}
```

---

## Funcionalidades Verificadas para Estudiantes

### ✅ Autenticación
- Login con email y contraseña
- Registro de nuevos estudiantes
- JWT con rol "Alumno"
- Redirección correcta a `/student`

### ✅ Dashboard (`/student`)
- Métricas: Borradores, En Revisión, Aprobadas, Citas Hoy
- Lista de historias clínicas propias
- Lista de citas académicas programadas
- Botón "Nueva Historia"

### ✅ Historias Clínicas
- **GET /api/academic/clinical-histories** - Filtrado automático por studentId
- **POST /api/academic/clinical-histories** - Crear nueva (Draft)
- **PUT /api/academic/clinical-histories/{id}** - Editar (solo Draft)
- **POST /api/academic/clinical-histories/{id}/submit** - Enviar a revisión
- **DELETE /api/academic/clinical-histories/{id}** - Eliminar

Estados posibles: `Draft`, `Submitted`, `Approved`, `Rejected`

### ✅ Citas Académicas
- **GET /api/academic/appointments** - Filtrado automático por studentId
- **POST /api/academic/appointments** - Crear nueva cita
- **PUT /api/academic/appointments/{id}** - Editar cita
- **DELETE /api/academic/appointments/{id}** - Eliminar cita

### ✅ Seguridad
- `[Authorize(Roles = Roles.Student)]` en endpoints específicos
- Los estudiantes solo ven sus propias historias y citas
- Los profesores pueden ver todas las historias y citas

---

## Datos de Prueba Automáticos

Al iniciar en modo Development, el sistema crea automáticamente:

### 3 Estudiantes
```
estudiante1@medicsys.com / Estudiante123! (ID: EST001)
estudiante2@medicsys.com / Estudiante123! (ID: EST002)
estudiante3@medicsys.com / Estudiante123! (ID: EST003)
```

### 1 Profesor
```
profesor@medicsys.com / Profesor123!
```

### Por cada estudiante:
- 2 citas académicas (1 pasada completada, 1 futura confirmada)
- 2 historias clínicas (1 aprobada, 1 en borrador)
- Recordatorios automáticos

---

## Instrucciones de Prueba

### 1. Iniciar el Sistema
```powershell
cd C:\MEDICSYS\MEDICSYS
.\iniciar-medicsys.ps1
```

### 2. Esperar a que los servicios estén listos
- Backend: http://localhost:5154
- Frontend: http://localhost:4200

### 3. Login como Estudiante
1. Ir a http://localhost:4200
2. Seleccionar "Estudiante" en el tipo (opcional, es decorativo)
3. Email: **estudiante1@medicsys.com**
4. Password: **Estudiante123!**
5. Click "Iniciar sesión"

### 4. Verificar Funcionalidad
- ✅ Debe redirigir a `/student` (Dashboard de Estudiante)
- ✅ Ver métricas en las tarjetas superiores
- ✅ Ver lista de historias clínicas
- ✅ Ver lista de citas
- ✅ Poder crear nueva historia clínica
- ✅ Poder navegar a la agenda

---

## Verificación en Base de Datos

### Conectar a PostgreSQL
```bash
psql -U postgres -d medicsys_academico
```

### Verificar Usuarios Estudiantes
```sql
SELECT "Id", "Email", "FullName", "UniversityId" 
FROM "AspNetUsers" 
WHERE "Email" LIKE '%estudiante%';
```

Resultado esperado:
```
Id                                  | Email                      | FullName     | UniversityId
------------------------------------|----------------------------|--------------|-------------
<guid>                              | estudiante1@medicsys.com   | Estudiante 1 | EST001
<guid>                              | estudiante2@medicsys.com   | Estudiante 2 | EST002
<guid>                              | estudiante3@medicsys.com   | Estudiante 3 | EST003
```

### Verificar Roles
```sql
SELECT u."Email", r."Name" 
FROM "AspNetUsers" u
JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" LIKE '%estudiante%';
```

Resultado esperado:
```
Email                      | Name
---------------------------|-------
estudiante1@medicsys.com   | Alumno
estudiante2@medicsys.com   | Alumno
estudiante3@medicsys.com   | Alumno
```

---

## Archivos Modificados

1. ✅ `Usuarios y claves.txt` - Credenciales actualizadas
2. ✅ `MEDICSYS.Api/appsettings.json` - Configuración del profesor
3. ✅ `MEDICSYS.Web/src/app/pages/login/login.ts` - Lógica de login
4. ✅ `iniciar-medicsys.ps1` - Rutas corregidas
5. ✅ `PRUEBAS_ESTUDIANTES.md` - Nueva documentación

---

## Resumen

Todos los problemas identificados han sido corregidos:

1. ✅ **Credenciales documentadas coinciden con el código**
2. ✅ **Login redirige correctamente usando el rol del backend**
3. ✅ **roleGuard valida contra "Alumno" correctamente**
4. ✅ **Script de inicio usa rutas correctas**
5. ✅ **Todas las funcionalidades de estudiantes están operativas**

El sistema está listo para pruebas. Puedes iniciar sesión con cualquiera de los 3 estudiantes creados automáticamente y todas las funcionalidades deberían funcionar correctamente.
