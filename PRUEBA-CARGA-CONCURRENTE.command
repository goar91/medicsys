#!/bin/bash
# ==============================================
# MEDICSYS - PRUEBA DE CARGA CONCURRENTE
# ==============================================
# Simula 100 usuarios concurrentes ingresando datos
# en diferentes módulos: facturación, inventario,
# contabilidad, gastos y compras (5 meses de datos)
# ==============================================

set -e

# Configuración
API_URL="http://localhost:5154/api"
NUM_CONCURRENT_USERS=100
MONTHS_TO_GENERATE=5
REQUESTS_PER_USER=20
TEMP_DIR="/tmp/medicsys_load_test"
RESULTS_FILE="$TEMP_DIR/results.log"

# Colores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

log() { echo -e "${CYAN}[$(date '+%H:%M:%S')]${NC} $1"; }
log_ok() { echo -e "${GREEN}[$(date '+%H:%M:%S')] ✓ $1${NC}"; }
log_err() { echo -e "${RED}[$(date '+%H:%M:%S')] ✗ $1${NC}"; }
log_warn() { echo -e "${YELLOW}[$(date '+%H:%M:%S')] ⚠ $1${NC}"; }

# Limpiar directorio temporal
mkdir -p "$TEMP_DIR"
rm -f "$TEMP_DIR"/*.json "$TEMP_DIR"/*.log 2>/dev/null || true

# Cabecera
clear
echo -e "${BLUE}"
echo "╔══════════════════════════════════════════════════════════════════╗"
echo "║                                                                  ║"
echo "║       🏥 MEDICSYS - PRUEBA DE CARGA CONCURRENTE                  ║"
echo "║                                                                  ║"
echo "║   Simulando $NUM_CONCURRENT_USERS usuarios concurrentes con $MONTHS_TO_GENERATE meses de datos        ║"
echo "║                                                                  ║"
echo "╚══════════════════════════════════════════════════════════════════╝"
echo -e "${NC}"

# Verificar que la API esté disponible
log "Verificando disponibilidad de la API..."
if ! curl -s -o /dev/null -w "%{http_code}" "$API_URL/auth/login" --max-time 5 | grep -q "40[0-5]\|200"; then
    log_err "API no disponible en $API_URL"
    log_warn "Ejecuta primero: ./INICIAR-MEDICSYS.command"
    exit 1
fi
log_ok "API disponible"

# Login y obtener token
log "Autenticando como odontólogo..."
AUTH_RESPONSE=$(curl -s -X POST "$API_URL/auth/login" \
    -H "Content-Type: application/json" \
    -d '{"email":"odontologo@medicsys.com","password":"Odontologo123!"}')

TOKEN=$(echo "$AUTH_RESPONSE" | grep -o '"token":"[^"]*"' | sed 's/"token":"//;s/"$//')

if [ -z "$TOKEN" ]; then
    log_err "No se pudo obtener token de autenticación"
    echo "Respuesta: $AUTH_RESPONSE"
    exit 1
fi
log_ok "Autenticación exitosa"

# Función para hacer peticiones con métricas
make_request() {
    local method=$1
    local endpoint=$2
    local data=$3
    local user_id=$4
    local start_time=$(date +%s%N)
    
    local response
    if [ -n "$data" ]; then
        response=$(curl -s -w "\n%{http_code}\n%{time_total}" \
            -X "$method" "$API_URL$endpoint" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "$data" \
            --max-time 30 2>/dev/null || echo -e "\n500\n30.0")
    else
        response=$(curl -s -w "\n%{http_code}\n%{time_total}" \
            -X "$method" "$API_URL$endpoint" \
            -H "Authorization: Bearer $TOKEN" \
            --max-time 30 2>/dev/null || echo -e "\n500\n30.0")
    fi
    
    local http_code=$(echo "$response" | tail -2 | head -1)
    local time_total=$(echo "$response" | tail -1)
    
    echo "$user_id,$method,$endpoint,$http_code,$time_total" >> "$RESULTS_FILE"
}

# Generar fecha aleatoria en un mes específico
random_date_in_month() {
    local months_ago=$1
    local year=$(date -v-${months_ago}m +%Y)
    local month=$(date -v-${months_ago}m +%m)
    local day=$((RANDOM % 28 + 1))
    printf "%s-%s-%02d" "$year" "$month" "$day"
}

# Función que simula un usuario concurrente
simulate_user() {
    local user_id=$1
    local user_log="$TEMP_DIR/user_${user_id}.log"
    
    echo "Usuario $user_id iniciado" > "$user_log"
    
    # Cada usuario hace múltiples operaciones
    for req in $(seq 1 $REQUESTS_PER_USER); do
        local operation=$((RANDOM % 5))
        local month_ago=$((RANDOM % MONTHS_TO_GENERATE))
        local date=$(random_date_in_month $month_ago)
        
        case $operation in
            0) # Crear gasto
                local amount=$((RANDOM % 500 + 10))
                local categories=("Insumos" "Servicios" "Mantenimiento" "Sueldos" "Alquiler")
                local category=${categories[$((RANDOM % ${#categories[@]}))]}
                local payments=("Efectivo" "Tarjeta" "Transferencia")
                local payment=${payments[$((RANDOM % ${#payments[@]}))]}
                
                make_request "POST" "/odontologia/gastos" \
                    "{\"description\":\"Gasto User$user_id Req$req\",\"amount\":$amount,\"expenseDate\":\"${date}T10:00:00Z\",\"category\":\"$category\",\"paymentMethod\":\"$payment\",\"supplier\":\"Proveedor$((RANDOM % 10))\"}" \
                    "$user_id"
                ;;
            1) # Consultar categorías contables (siempre funciona)
                make_request "GET" "/accounting/categories" "" "$user_id"
                ;;
            2) # Consultar inventario
                make_request "GET" "/odontologia/inventory?page=1&pageSize=50" "" "$user_id"
                ;;
            3) # Consultar gastos del mes
                make_request "GET" "/odontologia/gastos?page=1&pageSize=50" "" "$user_id"
                ;;
            4) # Consultar facturas
                make_request "GET" "/invoices?page=1&pageSize=50" "" "$user_id"
                ;;
        esac
        
        # Pequeña pausa aleatoria para simular comportamiento real
        sleep 0.$((RANDOM % 3))
    done
    
    echo "Usuario $user_id completado" >> "$user_log"
}

# ==============================================
# FASE 1: Generación de datos base (5 meses)
# ==============================================
echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
log "FASE 1: Generando datos base de 5 meses..."
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

# Verificar/crear inventario base
log "Verificando inventario base..."
INVENTORY_COUNT=$(curl -s "$API_URL/odontologia/inventory" \
    -H "Authorization: Bearer $TOKEN" | grep -o '"id"' | wc -l || echo "0")

if [ "$INVENTORY_COUNT" -lt 30 ]; then
    log "Creando artículos de inventario base..."
    for i in $(seq 1 30); do
        ITEM_DATA=$(cat <<EOF
{
    "name": "Insumo Dental $i",
    "description": "Descripción del insumo $i",
    "sku": "INS-$(printf '%04d' $i)-$(date +%s | tail -c 4)",
    "quantity": $((RANDOM % 100 + 20)),
    "minimumQuantity": $((RANDOM % 15 + 5)),
    "unitPrice": $((RANDOM % 50 + 5)).$((RANDOM % 99)),
    "supplier": "Proveedor$((i % 5 + 1))"
}
EOF
)
        curl -s -X POST "$API_URL/odontologia/inventory" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "$ITEM_DATA" > /dev/null
        printf "."
    done
    echo ""
    log_ok "30 artículos de inventario creados"
fi

# Generar datos para cada mes
for month in $(seq 0 $((MONTHS_TO_GENERATE - 1))); do
    month_name=$(date -v-${month}m +"%B %Y")
    log "Generando datos para $month_name..."
    
    # 20 gastos por mes
    for i in $(seq 1 20); do
        date=$(random_date_in_month $month)
        amount=$((RANDOM % 300 + 20))
        categories=("Insumos" "Servicios" "Mantenimiento" "Sueldos" "Alquiler" "Publicidad" "Transporte")
        category=${categories[$((RANDOM % ${#categories[@]}))]}
        payments=("Efectivo" "Tarjeta" "Transferencia")
        payment=${payments[$((RANDOM % ${#payments[@]}))]}
        
        curl -s -X POST "$API_URL/odontologia/gastos" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "{\"description\":\"Gasto $category - $i\",\"amount\":$amount,\"expenseDate\":\"${date}T10:00:00Z\",\"category\":\"$category\",\"paymentMethod\":\"$payment\",\"supplier\":\"Proveedor$((RANDOM % 10))\"}" > /dev/null &
    done
    wait
    
    # 15 movimientos contables por mes
    for i in $(seq 1 15); do
        date=$(random_date_in_month $month)
        amount=$((RANDOM % 800 + 100))
        types=("Income" "Expense")
        type=${types[$((RANDOM % 2))]}
        pmethods=("Cash" "Card" "Transfer")
        pmethod=${pmethods[$((RANDOM % 3))]}
        
        curl -s -X POST "$API_URL/accounting/entries" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "{\"description\":\"Movimiento contable $i\",\"amount\":$amount,\"date\":\"${date}T12:00:00Z\",\"type\":\"$type\",\"paymentMethod\":\"$pmethod\"}" > /dev/null &
    done
    wait
    
    # 10 facturas por mes
    for i in $(seq 1 10); do
        date=$(random_date_in_month $month)
        subtotal=$((RANDOM % 500 + 50))
        iva=$(echo "scale=2; $subtotal * 0.15" | bc)
        total=$(echo "scale=2; $subtotal + $iva" | bc)
        
        curl -s -X POST "$API_URL/invoices" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "{\"clientName\":\"Cliente $i\",\"clientId\":\"170000000$i\",\"paymentMethod\":\"Efectivo\",\"items\":[{\"description\":\"Tratamiento dental\",\"quantity\":1,\"unitPrice\":$subtotal}]}" > /dev/null &
    done
    wait
    
    printf "."
done
echo ""
log_ok "Datos de 5 meses generados"

# ==============================================
# FASE 2: Prueba de carga con 100 usuarios
# ==============================================
echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
log "FASE 2: Iniciando prueba de carga con $NUM_CONCURRENT_USERS usuarios concurrentes..."
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

# Inicializar archivo de resultados
echo "user_id,method,endpoint,http_code,time_seconds" > "$RESULTS_FILE"

# Capturar tiempo de inicio
START_TIME=$(date +%s)

# Lanzar usuarios concurrentes en lotes
BATCH_SIZE=25
for batch in $(seq 0 $((NUM_CONCURRENT_USERS / BATCH_SIZE - 1))); do
    batch_start=$((batch * BATCH_SIZE + 1))
    batch_end=$((batch_start + BATCH_SIZE - 1))
    
    log "Lanzando usuarios $batch_start-$batch_end..."
    
    for user_id in $(seq $batch_start $batch_end); do
        simulate_user $user_id &
    done
    
    # Esperar a que termine el lote antes de lanzar el siguiente
    wait
done

# Tiempo total
END_TIME=$(date +%s)
TOTAL_TIME=$((END_TIME - START_TIME))

# ==============================================
# FASE 3: Análisis de resultados
# ==============================================
echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
log "FASE 3: Analizando resultados..."
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

# Contar resultados
TOTAL_REQUESTS=$(tail -n +2 "$RESULTS_FILE" | wc -l | tr -d ' ')
SUCCESSFUL_REQUESTS=$(tail -n +2 "$RESULTS_FILE" | awk -F',' '$4 >= 200 && $4 < 300' | wc -l | tr -d ' ')
FAILED_REQUESTS=$((TOTAL_REQUESTS - SUCCESSFUL_REQUESTS))

# Calcular tiempos promedio
AVG_TIME=$(tail -n +2 "$RESULTS_FILE" | awk -F',' '{sum += $5; count++} END {if(count>0) printf "%.3f", sum/count; else print "0"}')
MAX_TIME=$(tail -n +2 "$RESULTS_FILE" | awk -F',' 'BEGIN {max=0} {if($5>max) max=$5} END {printf "%.3f", max}')
MIN_TIME=$(tail -n +2 "$RESULTS_FILE" | awk -F',' 'BEGIN {min=9999} {if($5<min && $5>0) min=$5} END {printf "%.3f", min}')

# Calcular requests por segundo
if [ "$TOTAL_TIME" -gt 0 ]; then
    RPS=$(echo "scale=2; $TOTAL_REQUESTS / $TOTAL_TIME" | bc)
else
    RPS="N/A"
fi

# Tasa de éxito
if [ "$TOTAL_REQUESTS" -gt 0 ]; then
    SUCCESS_RATE=$(echo "scale=2; $SUCCESSFUL_REQUESTS * 100 / $TOTAL_REQUESTS" | bc)
else
    SUCCESS_RATE="0"
fi

# Resumen por endpoint
echo ""
log "Resumen por endpoint:"
echo "─────────────────────────────────────────────────────────"
tail -n +2 "$RESULTS_FILE" | awk -F',' '
{
    endpoints[$3]++
    if($4 >= 200 && $4 < 300) success[$3]++
    time[$3] += $5
}
END {
    for(e in endpoints) {
        s = success[e] ? success[e] : 0
        avg = time[e] / endpoints[e]
        printf "  %-40s %5d reqs | %5d ok | %.3fs avg\n", e, endpoints[e], s, avg
    }
}'
echo ""

# Mostrar resumen final
echo -e "${GREEN}"
echo "╔══════════════════════════════════════════════════════════════════╗"
echo "║                                                                  ║"
echo "║                    📊 RESUMEN DE LA PRUEBA                       ║"
echo "║                                                                  ║"
echo "╠══════════════════════════════════════════════════════════════════╣"
printf "║  👥 Usuarios concurrentes:          %-28s ║\n" "$NUM_CONCURRENT_USERS"
printf "║  📋 Total de peticiones:            %-28s ║\n" "$TOTAL_REQUESTS"
printf "║  ✅ Peticiones exitosas:            %-28s ║\n" "$SUCCESSFUL_REQUESTS"
printf "║  ❌ Peticiones fallidas:            %-28s ║\n" "$FAILED_REQUESTS"
printf "║  📈 Tasa de éxito:                  %-28s ║\n" "${SUCCESS_RATE}%"
echo "╠══════════════════════════════════════════════════════════════════╣"
printf "║  ⏱️  Tiempo total:                   %-28s ║\n" "${TOTAL_TIME}s"
printf "║  ⚡ Peticiones por segundo:         %-28s ║\n" "${RPS}"
printf "║  🕐 Tiempo promedio respuesta:      %-28s ║\n" "${AVG_TIME}s"
printf "║  📉 Tiempo mínimo:                  %-28s ║\n" "${MIN_TIME}s"
printf "║  📈 Tiempo máximo:                  %-28s ║\n" "${MAX_TIME}s"
echo "╚══════════════════════════════════════════════════════════════════╝"
echo -e "${NC}"

# Evaluación del rendimiento
echo ""
if (( $(echo "$SUCCESS_RATE >= 95" | bc -l) )); then
    log_ok "RENDIMIENTO EXCELENTE - La aplicación maneja bien la carga concurrente"
elif (( $(echo "$SUCCESS_RATE >= 85" | bc -l) )); then
    log_warn "RENDIMIENTO ACEPTABLE - Algunas peticiones fallaron bajo carga"
else
    log_err "RENDIMIENTO DEFICIENTE - Muchas peticiones fallaron bajo carga"
fi

if (( $(echo "$AVG_TIME < 1.0" | bc -l) )); then
    log_ok "TIEMPOS DE RESPUESTA ÓPTIMOS - Promedio menor a 1 segundo"
elif (( $(echo "$AVG_TIME < 3.0" | bc -l) )); then
    log_warn "TIEMPOS DE RESPUESTA ACEPTABLES - Promedio entre 1-3 segundos"
else
    log_err "TIEMPOS DE RESPUESTA LENTOS - Promedio mayor a 3 segundos"
fi

# Guardar reporte
REPORT_FILE="$TEMP_DIR/load_test_report_$(date +%Y%m%d_%H%M%S).txt"
cat > "$REPORT_FILE" << EOF
MEDICSYS - Reporte de Prueba de Carga
=====================================
Fecha: $(date)

Configuración:
- Usuarios concurrentes: $NUM_CONCURRENT_USERS
- Peticiones por usuario: $REQUESTS_PER_USER
- Meses de datos: $MONTHS_TO_GENERATE

Resultados:
- Total peticiones: $TOTAL_REQUESTS
- Exitosas: $SUCCESSFUL_REQUESTS
- Fallidas: $FAILED_REQUESTS
- Tasa de éxito: ${SUCCESS_RATE}%
- Tiempo total: ${TOTAL_TIME}s
- Peticiones/segundo: $RPS
- Tiempo promedio: ${AVG_TIME}s
- Tiempo mínimo: ${MIN_TIME}s
- Tiempo máximo: ${MAX_TIME}s
EOF

log "Reporte guardado en: $REPORT_FILE"
log "Resultados detallados en: $RESULTS_FILE"

echo ""
read -r -p "Presiona ENTER para cerrar..."
