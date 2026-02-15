#!/bin/bash
# ==============================================
# MEDICSYS - INICIAR SISTEMA COMPLETO
# ==============================================
# Este script inicia todos los componentes:
# 1. Base de datos (PostgreSQL)
# 2. Backend (API .NET)
# 3. Frontend (Angular)
# Y abre automáticamente el navegador
# ==============================================

set -e

# Colores para la consola
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # Sin color

# Directorio del proyecto
ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT_DIR" || exit 1

# Función para mostrar mensajes con timestamp
log() {
    echo -e "${CYAN}[$(date '+%H:%M:%S')]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[$(date '+%H:%M:%S')] ✓ $1${NC}"
}

log_error() {
    echo -e "${RED}[$(date '+%H:%M:%S')] ✗ $1${NC}"
}

log_warning() {
    echo -e "${YELLOW}[$(date '+%H:%M:%S')] ⚠ $1${NC}"
}

# Verificar que Docker está corriendo
check_docker() {
    log "Verificando Docker..."
    if ! docker info > /dev/null 2>&1; then
        log_error "Docker no está corriendo. Por favor, inicia Docker Desktop."
        read -r -p "Presiona ENTER para cerrar..."
        exit 1
    fi
    log_success "Docker está corriendo"
}

# Esperar a que PostgreSQL esté healthy
wait_postgres() {
    log "Esperando a que la base de datos esté lista..."
    local attempts=0
    local max_attempts=60
    
    while [ $attempts -lt $max_attempts ]; do
        local status
        status=$(docker inspect -f '{{.State.Health.Status}}' medicsys-postgres 2>/dev/null || echo "not_found")
        
        if [ "$status" = "healthy" ]; then
            log_success "Base de datos lista"
            return 0
        fi
        
        attempts=$((attempts + 1))
        printf "."
        sleep 2
    done
    
    echo ""
    log_error "Timeout esperando la base de datos"
    return 1
}

# Esperar a que el backend esté listo
wait_backend() {
    log "Esperando a que el backend esté listo..."
    local attempts=0
    local max_attempts=90
    
    while [ $attempts -lt $max_attempts ]; do
        local code
        code=$(curl -sS -o /dev/null -w '%{http_code}' http://localhost:5154/api/health 2>/dev/null || echo "000")
        
        if [ "$code" = "200" ] || [ "$code" = "404" ]; then
            log_success "Backend listo en http://localhost:5154"
            return 0
        fi
        
        attempts=$((attempts + 1))
        printf "."
        sleep 2
    done
    
    echo ""
    log_warning "El backend podría estar iniciándose aún"
    return 0
}

# Esperar a que el frontend esté listo
wait_frontend() {
    log "Esperando a que el frontend esté listo..."
    local attempts=0
    local max_attempts=120
    
    while [ $attempts -lt $max_attempts ]; do
        local code
        code=$(curl -sS -o /dev/null -w '%{http_code}' http://localhost:4200 2>/dev/null || echo "000")
        
        if [ "$code" = "200" ]; then
            log_success "Frontend listo en http://localhost:4200"
            return 0
        fi
        
        attempts=$((attempts + 1))
        printf "."
        sleep 2
    done
    
    echo ""
    log_warning "El frontend podría estar iniciándose aún"
    return 0
}

# Cabecera
clear
echo -e "${BLUE}"
echo "╔══════════════════════════════════════════════════════════╗"
echo "║                                                          ║"
echo "║              🏥 MEDICSYS - INICIAR SISTEMA               ║"
echo "║                                                          ║"
echo "╚══════════════════════════════════════════════════════════╝"
echo -e "${NC}"
echo ""

# Verificaciones iniciales
check_docker

# Paso 1: Iniciar Base de Datos
echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
log "PASO 1: Iniciando Base de Datos (PostgreSQL)..."
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
docker compose up -d postgres
wait_postgres

# Paso 2: Iniciar Backend
echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
log "PASO 2: Iniciando Backend (API .NET)..."
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
docker compose up -d api
wait_backend

# Paso 3: Iniciar Frontend
echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
log "PASO 3: Iniciando Frontend (Angular)..."
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
docker compose up -d web
wait_frontend

# Paso 4: Abrir navegador
echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
log "PASO 4: Abriendo navegador..."
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
open "http://localhost:4200"
log_success "Navegador abierto con MEDICSYS"

# Resumen final
echo ""
echo -e "${GREEN}"
echo "╔══════════════════════════════════════════════════════════╗"
echo "║                                                          ║"
echo "║           ✓ SISTEMA INICIADO CORRECTAMENTE               ║"
echo "║                                                          ║"
echo "╠══════════════════════════════════════════════════════════╣"
echo "║                                                          ║"
echo "║   🌐 Frontend:  http://localhost:4200                    ║"
echo "║   🔧 Backend:   http://localhost:5154                    ║"
echo "║   🗄️  Database:  localhost:5433                          ║"
echo "║                                                          ║"
echo "╚══════════════════════════════════════════════════════════╝"
echo -e "${NC}"

echo ""
log "Estado de los contenedores:"
docker compose ps

echo ""
echo -e "${YELLOW}Para detener el sistema, ejecuta: DETENER-MEDICSYS.command${NC}"
echo ""
read -r -p "Presiona ENTER para cerrar esta ventana..."
