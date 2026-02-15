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
  "$@" >/dev/null 2>&1
  local code=$?
  if [ $code -ne 0 ]; then
    log "Aviso: $description devolvió código $code"
  fi
}

close_browsers_macos() {
  log "Cerrando navegadores con el sistema operativo..."
  osascript -e 'tell application "Google Chrome" to if it is running then quit' >/dev/null 2>&1 || true
  osascript -e 'tell application "Safari" to if it is running then quit' >/dev/null 2>&1 || true
  osascript -e 'tell application "Firefox" to if it is running then quit' >/dev/null 2>&1 || true
  osascript -e 'tell application "Microsoft Edge" to if it is running then quit' >/dev/null 2>&1 || true
}

echo "==========================================="
echo " MEDICSYS - DETENER TODO (macOS)"
echo "==========================================="
echo "Orden configurado: 1) Backend 2) Base de datos 3) Frontend"
echo

run_step "1) Deteniendo backend..." docker compose stop api
run_step "2) Deteniendo base de datos..." docker compose stop postgres
run_step "3) Deteniendo frontend..." docker compose stop web
close_browsers_macos

echo
log "Sistema detenido."
docker compose ps
echo
read -r -p "Presiona ENTER para cerrar esta ventana..."
