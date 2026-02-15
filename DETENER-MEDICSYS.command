#!/bin/bash
# ==============================================
# MEDICSYS - DETENER SISTEMA COMPLETO
# ==============================================
# Este script detiene todos los componentes:
# 1. Frontend (Angular)
# 2. Backend (API .NET)
# 3. Base de datos (PostgreSQL)
# Y cierra las pestañas del navegador de MEDICSYS
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

# Cerrar pestañas de MEDICSYS en navegadores
close_medicsys_tabs() {
    log "Cerrando pestañas de MEDICSYS en navegadores..."
    
    # Cerrar pestaña específica de localhost:4200 en Chrome
    osascript -e '
    tell application "Google Chrome"
        if it is running then
            repeat with w in windows
                set tabIndicesToClose to {}
                set tabIndex to 0
                repeat with t in tabs of w
                    set tabIndex to tabIndex + 1
                    if URL of t contains "localhost:4200" then
                        set end of tabIndicesToClose to tabIndex
                    end if
                end repeat
                repeat with i from (count tabIndicesToClose) to 1 by -1
                    close tab (item i of tabIndicesToClose) of w
                end repeat
            end repeat
        end if
    end tell
    ' 2>/dev/null || true
    
    # Cerrar pestaña específica de localhost:4200 en Safari
    osascript -e '
    tell application "Safari"
        if it is running then
            repeat with w in windows
                repeat with t in tabs of w
                    if URL of t contains "localhost:4200" then
                        close t
                    end if
                end repeat
            end repeat
        end if
    end tell
    ' 2>/dev/null || true
    
    # Cerrar pestaña específica de localhost:4200 en Firefox
    osascript -e '
    tell application "Firefox"
        if it is running then
            activate
            delay 0.5
            tell application "System Events"
                keystroke "w" using {command down}
            end tell
        end if
    end tell
    ' 2>/dev/null || true
    
    # Cerrar pestaña específica de localhost:4200 en Edge
    osascript -e '
    tell application "Microsoft Edge"
        if it is running then
            repeat with w in windows
                set tabIndicesToClose to {}
                set tabIndex to 0
                repeat with t in tabs of w
                    set tabIndex to tabIndex + 1
                    if URL of t contains "localhost:4200" then
                        set end of tabIndicesToClose to tabIndex
                    end if
                end repeat
                repeat with i from (count tabIndicesToClose) to 1 by -1
                    close tab (item i of tabIndicesToClose) of w
                end repeat
            end repeat
        end if
    end tell
    ' 2>/dev/null || true
    
    log_success "Pestañas de MEDICSYS cerradas"
}

# Verificar que Docker está corriendo
check_docker() {
    log "Verificando Docker..."
    if ! docker info > /dev/null 2>&1; then
        log_warning "Docker no está corriendo. No hay servicios que detener."
        read -r -p "Presiona ENTER para cerrar..."
        exit 0
    fi
    log_success "Docker está corriendo"
}

# Cabecera
clear
echo -e "${RED}"
echo "╔══════════════════════════════════════════════════════════╗"
echo "║                                                          ║"
echo "║              🛑 MEDICSYS - DETENER SISTEMA               ║"
echo "║                                                          ║"
echo "╚══════════════════════════════════════════════════════════╝"
echo -e "${NC}"
echo ""

# Verificaciones iniciales
check_docker

# Paso 1: Cerrar navegadores (pestañas de MEDICSYS)
echo ""
echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
log "PASO 1: Cerrando pestañas de MEDICSYS en navegadores..."
echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
close_medicsys_tabs

# Paso 2: Detener Frontend
echo ""
echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
log "PASO 2: Deteniendo Frontend (Angular)..."
echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
docker compose stop web 2>/dev/null || true
log_success "Frontend detenido"

# Paso 3: Detener Backend
echo ""
echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
log "PASO 3: Deteniendo Backend (API .NET)..."
echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
docker compose stop api 2>/dev/null || true
log_success "Backend detenido"

# Paso 4: Detener Base de Datos
echo ""
echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
log "PASO 4: Deteniendo Base de Datos (PostgreSQL)..."
echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
docker compose stop postgres 2>/dev/null || true
log_success "Base de datos detenida"

# Resumen final
echo ""
echo -e "${GREEN}"
echo "╔══════════════════════════════════════════════════════════╗"
echo "║                                                          ║"
echo "║           ✓ SISTEMA DETENIDO CORRECTAMENTE               ║"
echo "║                                                          ║"
echo "╚══════════════════════════════════════════════════════════╝"
echo -e "${NC}"

echo ""
log "Estado de los contenedores:"
docker compose ps

echo ""
echo -e "${YELLOW}Para iniciar el sistema nuevamente, ejecuta: INICIAR-MEDICSYS.command${NC}"
echo ""
read -r -p "Presiona ENTER para cerrar esta ventana..."
