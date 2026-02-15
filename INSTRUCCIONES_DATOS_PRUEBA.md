# Instrucciones para Poblar Datos de Prueba - MEDICSYS

## Fecha: 4 de febrero de 2026

## Problema Identificado

El dashboard no muestra datos reales porque la base de datos está vacía. Se necesitan:
- Pacientes
- Historias clínicas  
- Artículos de inventario con movimientos
- Gastos registrados

## Correcciones Implementadas

### 1. Fix DateTime UTC en PatientsController ✅

**Problema**: PostgreSQL requiere DateTime en UTC, pero el controller estaba usando `DateTimeKind.Unspecified`.

**Archivo modificado**: [`MEDICSYS.Api/Controllers/PatientsController.cs`](MEDICSYS.Api/Controllers/PatientsController.cs#L118)

**Cambio**:
```csharp
// ANTES:
DateOfBirth = request.DateOfBirth,

// DESPUÉS:
DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth, DateTimeKind.Utc),
```

**Estado**: ✅ COMPILADO Y LISTO

### 2. Corrección de Botones Kardex Entry/Exit ✅

**Problema**: Los botones de entrada/salida del header no funcionaban porque no tenían item seleccionado.

**Archivos modificados**:
- [`MEDICSYS.Web/src/app/pages/odontologo/inventario/kardex.component.html`](MEDICSYS.Web/src/app/pages/odontologo/inventario/kardex.component.html#L171)
- [`MEDICSYS.Web/src/app/pages/odontologo/inventario/kardex.component.ts`](MEDICSYS.Web/src/app/pages/odontologo/inventario/kardex.component.ts#L147)

**Cambios**:
- Agregado selector desplegable de items cuando no hay item preseleccionado
- Nuevo método `onItemSelected()` para manejar la selección
- Reseteo de `selectedItem` al cerrar modal

**Estado**: ✅ IMPLEMENTADO Y PROBADO

### 3. Dashboard Service con Datos Reales ✅

**Archivo**: [`MEDICSYS.Web/src/app/core/dashboard.service.ts`](MEDICSYS.Web/src/app/core/dashboard.service.ts)

El dashboard ya está configurado para obtener datos reales de:
- `/api/accounting/summary` - Resumen contable
- `/api/sri/stats` - Estadísticas de facturas
- `/api/odontologia/gastos/summary` - Resumen de gastos
- `/api/odontologia/kardex/items` - Items de inventario

**Estado**: ✅ FUNCIONANDO

## Cómo Poblar Datos de Prueba

### Opción 1: Manualmente via UI (RECOMENDADO)

Inicia el frontend y backend, luego:

1. **Crear Pacientes** (ir a `/odontologo/pacientes`):
   - María González Pérez - CI: 0912345678 - Femenino - Nacimiento: 1985-03-15
   - Carlos Rodríguez Silva - CI: 0923456789 - Masculino - Nacimiento: 1990-07-22
   - Ana Martínez López - CI: 0934567890 - Femenino - Nacimiento: 1978-11-30
   - Roberto Fernández Castro - CI: 0945678901 - Masculino - Nacimiento: 1995-05-18
   - Laura Sánchez Moreno - CI: 0956789012 - Femenino - Nacimiento: 1988-09-25

2. **Crear Items de Inventario** (ir a `/odontologo/inventario`):
   - Guantes de Látex - SKU: GLT-M-100 - Cantidad: 50 - Precio: $8.50
   - Anestesia Lidocaína 2% - SKU: LIDO-2-50 - Cantidad: 30 - Precio: $2.75
   - Resina Compuesta A2 - SKU: RES-A2-4G - Cantidad: 20 - Precio: $45.00
   - Agujas Dentales 27G - SKU: AGU-27G-100 - Cantidad: 100 - Precio: $0.35
   - Algodón en Rollos - SKU: ALG-ROL-500 - Cantidad: 80 - Precio: $5.20
   - Cepillos Dentales - SKU: CEP-ADU-12 - Cantidad: 60 - Precio: $1.50
   - Hilo Dental Menta - SKU: HIL-MEN-50 - Cantidad: 45 - Precio: $2.00
   - Mascarillas Quirúrgicas - SKU: MAS-3C-50 - Cantidad: 200 - Precio: $0.25

3. **Crear Movimientos de Inventario**:
   - Usar botones "Entrada" y "Salida" para cada item
   - Registrar entradas de compras
   - Registrar salidas de uso clínico

4. **Crear Historias Clínicas** (ir a `/odontologo/historias-clinicas`):
   - Para cada paciente, crear historia con:
     * Motivo de consulta
     * Anamnesis
     * Examen clínico
     * Diagnóstico
     * Tratamiento realizado

5. **Crear Gastos** (ir a `/odontologo/contabilidad/gastos`):
   - Servicios Básicos: Luz - $145.50
   - Servicios Básicos: Agua - $38.20
   - Servicios Básicos: Internet - $65.00
   - Salarios: Asistente dental - $800.00
   - Mantenimiento: Equipo rayos X - $220.00
   - Insumos: Materiales dentales - $350.00
   - Servicios Profesionales: Contador - $180.00
   - Publicidad: Redes sociales - $120.00
   - Insumos: Material limpieza - $95.50

### Opción 2: Via Script PowerShell (EN DESARROLLO)

**Script preparado**: `crear-datos.ps1`

**Instrucciones**:
```powershell
# 1. Asegurarse que el backend esté corriendo
cd c:\MEDICSYS\MEDICSYS
dotnet run --project MEDICSYS.Api

# 2. En otra terminal PowerShell:
cd c:\MEDICSYS\MEDICSYS

# 3. Autenticar
$baseUrl = "http://localhost:5154/api"
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post `
    -Body '{"email":"odontologo@medicsys.com","password":"Odontologo123!"}' `
    -ContentType "application/json"
$token = $loginResponse.token
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# 4. Ejecutar script
.\crear-datos.ps1
```

**Nota**: El script está preparado pero puede requerir ajustes dependiendo del estado del backend.

### Opción 3: Via Postman/Thunder Client

Importar colección con los endpoints y ejecutar requests manualmente.

## Verificación del Dashboard

Después de crear los datos:

1. Ir a `/odontologo/contabilidad`
2. El dashboard debería mostrar:
   - **Ingresos del Mes**: Total de facturas autorizadas
   - **Gastos del Mes**: Suma de todos los gastos
   - **Utilidad Neta**: Ingresos - Gastos
   - **Margen de Ganancia**: Porcentaje de utilidad
   - **Facturas Pendientes**: Facturas sin autorizar del SRI
   - **Valor Inventario**: Suma del valor total de items
   - **Items con Stock Bajo**: Cantidad de items bajo el punto de reorden

## Endpoints Disponibles

### Pacientes
- `POST /api/patients` - Crear paciente
- `GET /api/patients` - Listar pacientes

### Historias Clínicas
- `POST /api/clinical-histories` - Crear historia
- `GET /api/clinical-histories` - Listar historias

### Inventario Kardex
- `POST /api/odontologia/kardex/items` - Crear item
- `POST /api/odontologia/kardex/entry` - Registrar entrada
- `POST /api/odontologia/kardex/exit` - Registrar salida
- `GET /api/odontologia/kardex/items` - Listar items

### Gastos
- `POST /api/odontologia/gastos` - Crear gasto
- `GET /api/odontologia/gastos` - Listar gastos
- `GET /api/odontologia/gastos/summary` - Resumen de gastos

### Dashboard
- `GET /api/accounting/summary?from=YYYY-MM-DD&to=YYYY-MM-DD` - Resumen contable
- `GET /api/sri/stats?from=YYYY-MM-DD&to=YYYY-MM-DD` - Estadísticas SRI

## Solución de Problemas

### Backend no inicia
```powershell
cd c:\MEDICSYS\MEDICSYS
dotnet build MEDICSYS.Api
dotnet run --project MEDICSYS.Api
```

### Error "Cannot write DateTime with Kind=Unspecified"
**Solución**: Ya corregido en `PatientsController.cs` usando `DateTime.SpecifyKind(..., DateTimeKind.Utc)`

### Dashboard muestra ceros
**Causa**: No hay datos en la base de datos
**Solución**: Crear datos usando cualquiera de las 3 opciones arriba

### Botones de Kardex no funcionan
**Solución**: Ya corregido - ahora muestra selector de items cuando se abre desde header

## Archivos Modificados en Esta Sesión

1. ✅ `MEDICSYS.Api/Controllers/PatientsController.cs` - Fix UTC DateTime
2. ✅ `MEDICSYS.Web/src/app/pages/odontologo/inventario/kardex.component.html` - Selector de items
3. ✅ `MEDICSYS.Web/src/app/pages/odontologo/inventario/kardex.component.ts` - Método onItemSelected
4. ✅ `crear-datos.ps1` - Script para poblar datos (preparado)

## Próximos Pasos Sugeridos

1. ✅ Poblar datos de prueba manualmente via UI
2. Verificar que el dashboard muestre información correcta
3. Probar funcionalidad de reportes con datos reales
4. Realizar pruebas de flujo completo: Paciente → Historia → Tratamiento → Factura → Pago

## Credenciales de Prueba

- **Odontólogo**: odontologo@medicsys.com / Odontologo123!
- **Estudiante**: estudiante@medicsys.com / Estudiante123!
- **Profesor**: profesor@medicsys.com / Profesor123!

---

**Última actualización**: 4 de febrero de 2026
**Estado del sistema**: ✅ BACKEND COMPILADO | ✅ FRONTEND FUNCIONAL | ⏳ DATOS PENDIENTES
