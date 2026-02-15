#!/bin/bash

set -u

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT_DIR" || exit 1

log() {
  echo "[$(date '+%H:%M:%S')] $1"
}

run_step() {
  local description="$1"
  shift
  log "$description"
  "$@"
  local code=$?
  if [ $code -ne 0 ]; then
    log "ERROR ($code): $description"
    exit $code
  fi
}

wait_postgres_healthy() {
  log "Esperando base de datos en estado healthy..."
  local status=""
  for _ in $(seq 1 60); do
    status="$(docker inspect -f '{{.State.Health.Status}}' medicsys-postgres 2>/dev/null || true)"
    if [ "$status" = "healthy" ]; then
      log "Base de datos healthy."
      return 0
    fi
    sleep 2
  done
  log "ERROR: Timeout esperando PostgreSQL healthy."
  exit 1
}

wait_frontend() {
  log "Esperando frontend en http://localhost:4200 ..."
  local code=""
  for _ in $(seq 1 90); do
    code="$(curl -sS -o /dev/null -w '%{http_code}' http://localhost:4200 || true)"
    if [ "$code" = "200" ]; then
      log "Frontend disponible."
      return 0
    fi
    sleep 2
  done
  log "ERROR: Timeout esperando frontend."
  exit 1
}

echo "==========================================="
echo " MEDICSYS - EJECUTAR TODO (macOS)"
echo "==========================================="
echo "Orden configurado: 1) Backend 2) Base de datos 3) Frontend"
echo

run_step "1) Iniciando backend..." docker compose up -d --no-deps api
run_step "2) Iniciando base de datos..." docker compose up -d --no-deps postgres
wait_postgres_healthy
run_step "3) Iniciando frontend..." docker compose up -d --no-deps web
wait_frontend

log "Abriendo navegador con el sistema operativo..."
open "http://localhost:4200"

echo
log "Sistema iniciado correctamente."
docker compose ps
echo
read -r -p "Presiona ENTER para cerrar esta ventana..."
