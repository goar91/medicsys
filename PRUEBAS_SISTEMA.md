# 🧪 Guía de Pruebas del Sistema MEDICSYS

## ✅ Cambios Implementados

### 1. Componente de Inventario Angular
- ✅ **TypeScript** (`odontologo-inventario.ts`): Componente completo con signals, computed properties y métodos CRUD
- ✅ **HTML** (`odontologo-inventario.html`): Template con métricas, alertas, filtros y tabla de inventario
- ✅ **SCSS** (`odontologo-inventario.scss`): Estilos responsivos con animaciones y estados visuales
- ✅ **Rutas**: Agregada ruta `/odontologo/inventario` en `app.routes.ts`

### 2. Servicios Angular
- ✅ **InventoryService**: Servicio completo para CRUD de inventario y alertas
- ✅ **AcademicService**: Servicio para citas académicas, historias y recordatorios

### 3. Dashboards Actualizados
- ✅ **Dashboard Odontólogo**: Métricas de inventario, ingresos mensuales, alertas
- ✅ **Dashboard Profesor**: Métricas de historias pendientes, aprobadas, citas
- ✅ **Dashboard Estudiante**: Métricas de borradores, en revisión, aprobadas, citas

### 4. Backend Completo
- ✅ **API de Inventario**: CRUD completo + sistema de alertas automáticas
- ✅ **Dos bases de datos**: `medicsys_odontologia` y `medicsys_academico`
- ✅ **Datos de prueba**: 5 pacientes, 3 estudiantes, 7 items inventario, 5 alertas

## 🔐 Credenciales de Prueba

### Odontólogo
- **Email**: odontologo@medicsys.com
- **Contraseña**: Odontologo123!
- **Rol**: Odontologo
- **Acceso**: Pacientes, Citas, Historias, Facturación, Contabilidad, Inventario

### Profesor
- **Email**: profesor@medicsys.com
- **Contraseña**: Profesor123!
- **Rol**: Professor
- **Acceso**: Historias para revisar, Citas académicas, Agenda

### Estudiante 1
- **Email**: estudiante1@medicsys.com
- **Contraseña**: Estudiante123!
- **Rol**: Student
- **Acceso**: Crear historias, Ver citas, Agenda

### Estudiante 2
- **Email**: estudiante2@medicsys.com
- **Contraseña**: Estudiante123!
- **Rol**: Student

### Estudiante 3
- **Email**: estudiante3@medicsys.com
- **Contraseña**: Estudiante123!
- **Rol**: Student

## 🧪 Plan de Pruebas

### Prueba 1: Login como Odontólogo
1. Abrir http://localhost:4200
2. Login con `odontologo@medicsys.com` / `Odontologo123!`
3. **Verificar**:
   - ✅ Redirección a `/odontologo/dashboard`
   - ✅ Métricas visibles: Citas Hoy, Pacientes Activos, Ingresos del Mes, Alertas Inventario
   - ✅ Alertas de inventario en tiempo real
   - ✅ Botón "Inventario" en Acciones Rápidas

### Prueba 2: Navegación del Odontólogo
Desde el dashboard, hacer clic en cada sección:

#### a) Inventario
1. Click en "Inventario" (Acciones Rápidas)
2. **Verificar**:
   - ✅ URL: `/odontologo/inventario`
   - ✅ Métricas: Total de Items (7), Stock Bajo, Agotados, Por Expirar
   - ✅ Alertas activas visibles (5 alertas)
   - ✅ Tabla con 7 items:
     - Guantes látex (Stock Bajo - 8/10)
     - Mascarillas quirúrgicas (Agotado - 0/15)
     - Amalgama dental (OK - 25/10)
     - Anestesia local (Expirando)
     - Resina compuesta (OK - 30/5)
     - Hilo dental (OK - 100/20)
     - Ácido grabador (Expirado + Stock Bajo)
   - ✅ Filtros funcionales: Todos, Stock Bajo, Por Expirar
   - ✅ Botones: Actualizar, Nuevo Artículo, Editar, Eliminar

#### b) Pacientes
1. Click en "Pacientes"
2. **Verificar**:
   - ✅ URL: `/odontologo/pacientes`
   - ✅ Lista de 5 pacientes
   - ✅ Botón "Nuevo Paciente"

#### c) Historias Clínicas
1. Click en "Historias"
2. **Verificar**:
   - ✅ URL: `/odontologo/historias`
   - ✅ Lista de historias clínicas

#### d) Agenda
1. Click en "Agenda"
2. **Verificar**:
   - ✅ URL: `/odontologo/agenda`
   - ✅ Citas del odontólogo
   - ✅ Botón "Nueva Cita"

#### e) Facturación
1. Click en "Facturación"
2. **Verificar**:
   - ✅ URL: `/odontologo/facturacion`
   - ✅ Lista de 3 facturas
   - ✅ Totales correctos
   - ✅ Botón "Nueva Factura"

#### f) Contabilidad
1. Click en "Contabilidad"
2. **Verificar**:
   - ✅ URL: `/odontologo/contabilidad`
   - ✅ Entradas contables (6 entradas)
   - ✅ Resumen financiero
   - ✅ 5 categorías: Ingresos Consultas, Ingresos Tratamientos, Gastos Suministros, Gastos Laboratorio, Impuestos

### Prueba 3: Login como Profesor
1. Logout del odontólogo
2. Login con `profesor@medicsys.com` / `Profesor123!`
3. **Verificar**:
   - ✅ Redirección a `/professor`
   - ✅ Métricas: Historias Pendientes (2), Historias Aprobadas (1), Citas Hoy, Total Historias (6)
   - ✅ Lista de historias para revisar (6 historias)
   - ✅ Filtros por estado: Todas, Enviadas, Aprobadas, Rechazadas
   - ✅ Botones: Revisar, Eliminar

### Prueba 4: Revisar Historia (Profesor)
1. Click en "Revisar" de una historia con estado "Submitted"
2. **Verificar**:
   - ✅ URL: `/professor/histories/{id}`
   - ✅ Detalles de la historia visible
   - ✅ Datos del paciente
   - ✅ Botones: Aprobar, Rechazar, Volver

### Prueba 5: Login como Estudiante
1. Logout del profesor
2. Login con `estudiante1@medicsys.com` / `Estudiante123!`
3. **Verificar**:
   - ✅ Redirección a `/student`
   - ✅ Métricas: Borradores, En Revisión, Aprobadas, Citas Hoy
   - ✅ Lista de historias del estudiante
   - ✅ Botón "Nueva Historia"
   - ✅ Filtro por estado

### Prueba 6: Crear Historia (Estudiante)
1. Click en "Nueva Historia"
2. **Verificar**:
   - ✅ URL: `/student/histories/new`
   - ✅ Formulario de historia clínica
   - ✅ Campos: Paciente, Diagnóstico, Tratamiento, Observaciones
   - ✅ Botones: Guardar como Borrador, Enviar a Revisión

### Prueba 7: Funcionalidad de Alertas de Inventario
1. Login como odontólogo
2. Ir a Inventario
3. **Verificar alertas específicas**:
   - ✅ Alerta "Agotado": Mascarillas quirúrgicas (0 unidades)
   - ✅ Alerta "Stock Bajo": Guantes látex (8/10), Ácido grabador (3/5)
   - ✅ Alerta "Por Expirar": Anestesia local (expira en 15 días)
   - ✅ Alerta "Expirado": Ácido grabador (expiró hace 5 días)
4. **Probar resolver alerta**:
   - Click en "Resolver" de una alerta
   - Verificar que desaparece de la lista de alertas activas

### Prueba 8: Filtros de Inventario
1. En página de inventario:
2. **Filtro "Stock Bajo"**:
   - ✅ Muestra solo: Guantes látex, Mascarillas, Ácido grabador
3. **Filtro "Por Expirar"**:
   - ✅ Muestra solo: Anestesia local, Ácido grabador
4. **Filtro "Todos"**:
   - ✅ Muestra los 7 items

### Prueba 9: Verificar Backend API
URLs para probar en navegador o Postman:

#### Autenticación
```
POST http://localhost:5154/api/auth/login
Body: {
  "email": "odontologo@medicsys.com",
  "password": "Odontologo123!"
}
```

#### Inventario (requiere token)
```
GET http://localhost:5154/api/odontologia/inventory
Headers: Authorization: Bearer {token}
```

#### Alertas de Inventario
```
GET http://localhost:5154/api/odontologia/inventory/alerts?isResolved=false
Headers: Authorization: Bearer {token}
```

#### Citas Académicas
```
GET http://localhost:5154/api/academic/appointments
Headers: Authorization: Bearer {token}
```

#### Historias Académicas
```
GET http://localhost:5154/api/academic/clinical-histories
Headers: Authorization: Bearer {token}
```

## 🐛 Errores Conocidos a Verificar

### Frontend
- ✅ Sin errores TypeScript en componentes
- ✅ Rutas correctamente configuradas
- ✅ Guards de autenticación funcionando
- ✅ Servicios usando API_BASE_URL correctamente

### Backend
- ✅ Aplicación corriendo en http://localhost:5154
- ✅ Migraciones aplicadas correctamente
- ✅ Worker de recordatorios ejecutándose
- ⚠️ 1 warning: InvoiceItem.InvoiceId1 shadow property (no afecta funcionamiento)

## 📊 Datos de Prueba en Base de Datos

### Base `medicsys_odontologia`
- **Odontólogo**: 1 (Guid: generado)
- **Pacientes**: 5 (Juan Pérez, María García, Carlos López, Ana Martínez, Luis Rodríguez)
- **Citas**: 10 (variadas fechas y horarios)
- **Historias**: 5 (una por paciente)
- **Facturas**: 3 (con items y totales)
- **Categorías Contables**: 5
- **Entradas Contables**: 6
- **Items Inventario**: 7
  1. Guantes látex - Stock bajo (8/10)
  2. Mascarillas quirúrgicas - Agotado (0/15)
  3. Amalgama dental - OK (25/10)
  4. Anestesia local - Expirando (50/20)
  5. Resina compuesta - OK (30/5)
  6. Hilo dental - OK (100/20)
  7. Ácido grabador - Expirado + Bajo (3/5)
- **Alertas**: 5
  1. LowStock - Guantes látex
  2. OutOfStock - Mascarillas
  3. ExpirationWarning - Anestesia
  4. Expired - Ácido grabador (expirado)
  5. LowStock - Ácido grabador (bajo stock)

### Base `medicsys_academico`
- **Profesor**: 1 (Juan Pérez Profesor)
- **Estudiantes**: 3 (Juan, María, Carlos Estudiante)
- **Pacientes**: 6 (simulados para prácticas)
- **Citas Académicas**: 6
- **Historias Académicas**: 6 (2 Draft, 2 Submitted, 1 Approved, 1 Rejected)
- **Recordatorios**: 12 (algunos vencidos para pruebas)

## 🚀 Próximos Pasos Sugeridos

1. **Implementar formulario de creación/edición de items de inventario**
   - Componente modal o página separada
   - Validaciones de formulario
   - Integración con InventoryService

2. **Agregar exportación de reportes**
   - Exportar inventario a PDF/Excel
   - Reporte de items por expirar
   - Reporte de movimientos de inventario

3. **Notificaciones en tiempo real**
   - SignalR para alertas push
   - Notificaciones de stock bajo
   - Alertas de expiración próxima

4. **Historial de movimientos de inventario**
   - Modelo InventoryTransaction
   - Registro de entradas/salidas
   - Reporte de consumo

5. **Dashboard de reportes avanzados**
   - Gráficas de consumo
   - Predicción de reposición
   - Análisis de costos

## 📝 Notas Importantes

- **Separación total**: Los sistemas Odontológico y Académico son completamente independientes
- **Sin FK cruzadas**: No hay foreign keys entre las dos bases de datos
- **GUIDs para referencias**: Se usan GUIDs para referenciar usuarios sin validación de FK
- **Worker activo**: El ReminderWorker consulta cada 60 segundos los recordatorios pendientes
- **Alertas automáticas**: El sistema crea alertas automáticamente al crear/actualizar items

## 🎯 Checklist de Pruebas

### Login y Autenticación
- [ ] Login odontólogo exitoso
- [ ] Login profesor exitoso
- [ ] Login estudiante exitoso
- [ ] Logout funcional
- [ ] Redirección correcta según rol
- [ ] Guards bloqueando accesos no autorizados

### Navegación Odontólogo
- [ ] Dashboard carga correctamente
- [ ] Inventario muestra 7 items
- [ ] Pacientes muestra 5 pacientes
- [ ] Historias carga correctamente
- [ ] Agenda muestra citas
- [ ] Facturación muestra 3 facturas
- [ ] Contabilidad muestra entradas

### Funcionalidad Inventario
- [ ] Métricas correctas (7 total, stock bajo, agotados, expirando)
- [ ] Alertas activas visibles (5 alertas)
- [ ] Filtros funcionan correctamente
- [ ] Botón resolver alerta funciona
- [ ] Tabla responsive en móvil
- [ ] Colores y badges correctos por estado

### Navegación Profesor
- [ ] Dashboard carga con métricas correctas
- [ ] Lista de historias para revisar (6)
- [ ] Filtros por estado funcionan
- [ ] Botón revisar redirige correctamente

### Navegación Estudiante
- [ ] Dashboard carga con métricas
- [ ] Lista de historias propias
- [ ] Botón nueva historia funciona
- [ ] Filtros por estado funcionan

### API Backend
- [ ] Login retorna token JWT
- [ ] Endpoints de inventario funcionan
- [ ] Endpoints académicos funcionan
- [ ] Autorización por roles funciona
- [ ] Worker de recordatorios activo
