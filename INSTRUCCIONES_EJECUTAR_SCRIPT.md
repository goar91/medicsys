# Instrucciones para Ejecutar el Script de Generación de Datos

## Pasos para ejecutar correctamente:

### 1. Abrir DOS Terminales PowerShell Separadas

**Terminal 1 - Para el Backend:**
```powershell
cd C:\MEDICSYS\MEDICSYS\MEDICSYS.Api
dotnet run
```
Espera hasta ver el mensaje: "Now listening on: http://localhost:5154"

**Terminal 2 - Para ejecutar el script:**
```powershell
cd C:\MEDICSYS\MEDICSYS
.\generar-datos.ps1
```

### 2. ¿Qué hace el script?

El script `generar-datos.ps1` creará:
- ✅ 50 pacientes con datos ecuatorianos realistas
- ✅ 50 historias clínicas completas
- ✅ 50 facturas con 1-4 tratamientos cada una
- ✅ 30 movimientos contables (20 gastos + 10 ingresos)

### 3. Datos creados:

**Pacientes:**
- Nombres ecuatorianos (María, José, etc.)
- Cédulas válidas (170xxxxxxx formato EC)
- Direcciones en Quito
- Información médica completa

**Historias Clínicas:**
- Motivos de consulta variados
- Diagnósticos odontológicos
- Tratamientos especializados
- Observaciones detalladas

**Facturas:**
- Tratamientos: Limpieza ($45), Empaste ($65), Extracción ($85), Ortodoncia ($250), etc.
- IVA del 15% aplicado
- Descuentos (0%, 5%, 10%)
- Métodos de pago: Efectivo, Tarjeta, Transferencia, Cheque

**Contabilidad:**
- **Gastos:** Materiales odontológicos, servicios públicos, mantenimiento, etc.
- **Ingresos:** Consultas, radiografías, certificados

### 4. Verificar los datos:

Después de ejecutar el script, inicia sesión en:
```
http://localhost:4200
```

Usuarios de prueba:
- **Odontólogo:** odontologo1@medicsys.com / Odontologo123!
- **Estudiante:** estudiante1@medicsys.com / Estudiante123!

Navega a:
- **Pacientes:** Ver los 50 pacientes nuevos
- **Historias Clínicas:** Ver las 50 historias
- **Facturación:** Ver las 50 facturas generadas
- **Contabilidad:** Ver los movimientos contables

### 5. Si hay errores de conexión:

Si ves el error "No es posible conectar con el servidor remoto":
1. Verifica que el backend esté corriendo en la Terminal 1
2. Comprueba que veas "Now listening on: http://localhost:5154"
3. Espera 10-15 segundos después de iniciar el backend
4. Ejecuta de nuevo el script en la Terminal 2

### 6. Resumen esperado al finalizar:

```
=======================================
  RESUMEN
=======================================
Pacientes:     50
Historias:     50
Facturas:      50
Movimientos:   30
Total facturado: $X,XXX.XX

Proceso completado!
```

## Notas importantes:

- El script usa `$ErrorActionPreference = "Continue"` para continuar aunque falle alguna llamada
- Cada 10 registros se muestra el progreso
- Los datos se crean en el orden: Pacientes → Historias → Facturas → Contabilidad
- Se requiere autenticación (el script hace login automáticamente)

## ¿Necesitas más datos?

Para crear más datos, ejecuta el script múltiples veces. Los nuevos datos se agregarán a los existentes.
