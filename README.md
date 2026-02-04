# MEDICSYS

Plataforma para historias clinicas universitarias con flujo alumno-profesor.

## Requisitos
- .NET SDK 9
- Node.js (LTS)
- PostgreSQL (latest)

## Estrategia de Base de Datos (2026-02)
Para preservar todos los datos y evitar pÃ©rdidas, se mantiene una base de datos separada por mÃ³dulo:
- `medicsys` (core: agenda, pacientes, historias clÃ­nicas, recordatorios)
- `medicsys_academico` (academic + identidad/roles)
- `medicsys_odontologia` (odontologÃ­a: inventario, contabilidad, facturaciÃ³n, pacientes odontolÃ³gicos)

Esta separaciÃ³n evita migraciones destructivas y mantiene los datos existentes. La API sincroniza usuarios desde el contexto acadÃ©mico hacia el core para garantizar consistencia de nombres y correos.

## Backend (API)
1) Inicia PostgreSQL e instala la base `medicsys`.
2) Configura la cadena de conexion si aplica: `MEDICSYS.Api/appsettings.json`.
3) Restaura herramientas:
   - `dotnet tool restore`
4) Aplica migraciones:
   - `dotnet ef database update -p MEDICSYS.Api -s MEDICSYS.Api`
   - Si cambias los modelos, crea una nueva migracion con `dotnet ef migrations add <Nombre> -p MEDICSYS.Api -s MEDICSYS.Api`
5) Ejecuta la API:
   - `dotnet run --project MEDICSYS.Api`

Credenciales iniciales (profesor):
- Email: `profesor@medicsys.local`
- Password: `Medicsys#2026`

## Frontend (Angular)
1) `cd MEDICSYS.Web`
2) `npm install`
3) `npm start`

Por defecto el frontend apunta a `http://localhost:5154/api`.
Si cambias el puerto de la API, actualiza `MEDICSYS.Web/src/app/core/api.config.ts`.

## Seguridad
Cambia la llave JWT en `MEDICSYS.Api/appsettings.json` antes de desplegar.

## Flujo
- Alumno: registra cuenta, llena historia clinica, envia a revision.
- Profesor: revisa, aprueba o rechaza con observaciones.
