# MEDICSYS

Plataforma para historias clinicas universitarias con flujo alumno-profesor.

## Requisitos
- Docker + Docker Compose

## Estrategia de Base de Datos (2026-02)
Para preservar todos los datos y evitar pÃ©rdidas, se mantiene una base de datos separada por mÃ³dulo:
- `medicsys` (core: agenda, pacientes, historias clÃ­nicas, recordatorios)
- `medicsys_academico` (academic + identidad/roles)
- `medicsys_odontologia` (odontologÃ­a: inventario, contabilidad, facturaciÃ³n, pacientes odontolÃ³gicos)

Esta separaciÃ³n evita migraciones destructivas y mantiene los datos existentes.

## Ejecutar con Docker Compose
```bash
docker compose up -d
```

Servicios:
- Frontend: `http://localhost:4200`
- API: `http://localhost:5154`
- Health API: `http://localhost:5154/health`
- PostgreSQL host: `localhost:5433`

## Panel con 2 botones (Ejecutar Todo / Detener Todo)
Ejecuta:

```bash
python3 scripts/medicsys_control.py
```

Panel local:
- `http://127.0.0.1:8765`

El panel respeta este orden:
1. Backend
2. Base de datos
3. Frontend

Y además:
- Al iniciar: abre el navegador con `http://localhost:4200`
- Al detener: cierra navegador según el sistema operativo

## Pruebas de rendimiento API
```bash
python3 scripts/perf_api.py --base-url http://localhost:5154 --requests 30 --concurrency 6 --warmup 2
```

Reporte generado en:
- `artifacts/perf/*.json`
- `artifacts/perf/*.csv`

## Flujo
- Alumno: registra cuenta, llena historia clinica, envia a revision.
- Profesor: revisa, aprueba o rechaza con observaciones.
