# MEDICSYS

Plataforma para historias clinicas universitarias con flujo alumno-profesor.

## Requisitos
- .NET SDK 9
- Node.js (LTS)
- PostgreSQL (latest) o Docker

## Backend (API)
1) Inicia PostgreSQL:
   - Con Docker:
     - `docker compose up -d`
   - O instala PostgreSQL y crea la base `medicsys`.
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
