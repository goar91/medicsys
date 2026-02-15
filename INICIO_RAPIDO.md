# INICIO RÁPIDO - Verificación y Balance Contable MEDICSYS

## 🚀 Ejecución Rápida (3 pasos)

### 1️⃣ Iniciar Backend
```powershell
cd C:\MEDICSYS\MEDICSYS\MEDICSYS.Api
dotnet run
```
Esperar hasta ver: `Now listening on: http://localhost:5154`

### 2️⃣ Generar Datos y Balance
En **otra terminal PowerShell**:
```powershell
cd C:\MEDICSYS\MEDICSYS
.\ejecutar-todo.ps1
```

### 3️⃣ Iniciar Frontend (Opcional)
En **otra terminal**:
```powershell
cd C:\MEDICSYS\MEDICSYS\MEDICSYS.Web
npm start
```
Abrir: http://localhost:4200

---

## 📝 Qué hace `ejecutar-todo.ps1`

1. Verifica que el backend esté corriendo ✅
2. Muestra estado actual del sistema 📊
3. Genera **150 pacientes** + **150 historias** + **130 facturas** + **120 movimientos** 🏥
4. Crea balance contable de **4 meses** (Oct 2025 - Ene 2026) 💰
5. Exporta reporte a `balance-contable.txt` 📄

**Tiempo estimado:** 5-10 minutos

---

## 🎯 Usuarios de Prueba

| Usuario | Email | Password | Rol |
|---------|-------|----------|-----|
| Odontólogo | odontologo1@medicsys.com | Odontologo123! | Crear pacientes y facturas |
| Estudiante | estudiante1@medicsys.com | Estudiante123! | Crear historias clínicas |

---

## 📊 Datos que se Generan

- **Octubre 2025:** 30 pacientes, 25 facturas, 23 movimientos
- **Noviembre 2025:** 35 pacientes, 30 facturas, 28 movimientos
- **Diciembre 2025:** 40 pacientes, 35 facturas, 32 movimientos
- **Enero 2026:** 45 pacientes, 40 facturas, 37 movimientos

**Total:** 150 pacientes, 130 facturas, 120 movimientos contables

---

## 📄 Archivos Importantes

| Archivo | Descripción |
|---------|-------------|
| `ejecutar-todo.ps1` | Script maestro - ejecuta todo el proceso |
| `datos-4-meses.ps1` | Genera 4 meses de datos |
| `verificar-datos.ps1` | Verifica sistema y muestra resumen |
| `balance-contable.ps1` | Genera balance contable |
| `balance-contable.txt` | Reporte generado (después de ejecutar) |
| `RESUMEN_FINAL_VERIFICACION.md` | Documentación completa |

---

## ⚠️ Problemas Comunes

**Error: Backend no responde**
```powershell
# Solución: Iniciar backend primero
cd C:\MEDICSYS\MEDICSYS\MEDICSYS.Api
dotnet run
```

**Error 401 (No autorizado)**
- Los usuarios no existen
- Ejecutar primero el seeder del backend
- Verificar que PostgreSQL esté corriendo

**Script se ejecuta muy lento**
- Es normal, está creando 150+ registros
- Esperar pacientemente 5-10 minutos

---

## 📚 Más Información

- **Documentación completa:** [RESUMEN_FINAL_VERIFICACION.md](RESUMEN_FINAL_VERIFICACION.md)
- **Optimizaciones:** [INFORME_VERIFICACION_Y_OPTIMIZACION.md](INFORME_VERIFICACION_Y_OPTIMIZACION.md)

---

## ✅ Verificación Final

Después de ejecutar todo:
1. ✅ Ver `balance-contable.txt` generado
2. ✅ Abrir http://localhost:4200 y verificar Dashboard
3. ✅ Ver que pacientes, historias y facturas se muestran
4. ✅ Ir a Contabilidad y ver movimientos por mes

---

**¿Listo? Ejecuta `.\ejecutar-todo.ps1` y listo!** 🎉
