# Módulo de Facturación Electrónica - MEDICSYS

## ✅ Implementación Completa

### Descripción General
Sistema completo de facturación electrónica con integración al SRI (Servicio de Rentas Internas) de Ecuador. Permite generar facturas electrónicas, enviarlas automáticamente al SRI para su autorización, y gestionar todo el ciclo de facturación del consultorio dental.

---

## 🎯 Funcionalidades Implementadas

### 1. **Vista de Listado de Facturas** (`odontologo-facturacion`)
- ✅ Tabla completa con todas las facturas emitidas
- ✅ Métricas en tiempo real:
  - Facturas autorizadas
  - Facturas pendientes de envío
  - Total facturado
- ✅ Filtros por estado (Autorizadas/Pendientes/Rechazadas)
- ✅ Información detallada por factura:
  - Número de factura (001-001-000000001)
  - Fecha de emisión
  - Cliente (nombre e identificación)
  - Desglose de montos (Subtotal, IVA, Total)
  - Forma de pago (Efectivo/Tarjeta/Transferencia)
  - Estado SRI (Autorizada/Pendiente/Rechazada)
  - Número de autorización SRI
- ✅ Acciones por factura:
  - Ver detalle completo
  - Descargar PDF
  - Reenviar al SRI (para facturas pendientes)
- ✅ Botón "Nueva Factura"
- ✅ Exportación de facturas

### 2. **Formulario de Nueva Factura** (`odontologo-factura-form`)
- ✅ **Selección de Cliente:**
  - Opción "Consumidor Final" (un clic)
  - Opción "Nuevo Cliente" (formulario completo)
  - Clientes frecuentes (cards con avatar)
  - Campos cliente:
    - Tipo de identificación (RUC/Cédula/Pasaporte/Consumidor Final)
    - Número de identificación
    - Nombre/Razón Social
    - Dirección
    - Teléfono
    - Email (para envío automático)

- ✅ **Detalle de Items/Servicios:**
  - Grid dinámico de items
  - Servicios odontológicos predefinidos (10 servicios comunes):
    - Consulta General ($35.00)
    - Limpieza Dental ($45.00)
    - Extracción Simple ($50.00)
    - Extracción Compleja ($85.00)
    - Resina Dental ($65.00)
    - Endodoncia ($150.00)
    - Corona ($320.00)
    - Implante Dental ($850.00)
    - Ortodoncia - Mensualidad ($120.00)
    - Blanqueamiento Dental ($180.00)
  - Campos por item:
    - Descripción (con tags de servicios rápidos)
    - Cantidad
    - Precio unitario
    - Descuento %
    - Subtotal (calculado automáticamente)
  - Agregar/Eliminar items dinámicamente
  - Mínimo 1 item obligatorio

- ✅ **Forma de Pago:**
  - Radio cards visuales:
    - Efectivo (código SRI: 01)
    - Tarjeta (código SRI: 19)
    - Transferencia (código SRI: 20)
  - Campo observaciones opcional

- ✅ **Resumen de Totales:**
  - Subtotal calculado
  - IVA 15% (Ecuador)
  - Total
  - Nota informativa sobre envío automático al SRI

- ✅ **Validaciones:**
  - Formularios reactivos con validación
  - Campos requeridos marcados
  - Validación de email
  - Validación de montos
  - Prevención de eliminar último item

- ✅ **Acciones:**
  - Guardar y Enviar al SRI (botón primario)
  - Cancelar (con confirmación)

### 3. **Servicio de Integración SRI** (`sri.service.ts`)
- ✅ **Configuración Multi-ambiente:**
  - Ambiente de Pruebas (celcer.sri.gob.ec)
  - Ambiente de Producción (cel.sri.gob.ec)
  - URLs de Web Services oficiales del SRI

- ✅ **Generación de Clave de Acceso:**
  - Algoritmo de 49 dígitos según normativa SRI
  - Componentes:
    - Fecha de emisión (ddmmyyyy)
    - Tipo de comprobante (01 = Factura)
    - RUC del emisor
    - Ambiente (1=Pruebas, 2=Producción)
    - Establecimiento (001)
    - Punto de emisión (001)
    - Secuencial (9 dígitos)
    - Código numérico (8 dígitos aleatorios)
    - Tipo de emisión (1=Normal)
    - Dígito verificador (Módulo 11)

- ✅ **Construcción de XML:**
  - Formato según esquema XSD oficial del SRI
  - Estructura completa:
    - infoTributaria
    - infoFactura
    - detalles
    - infoAdicional
  - Escape de caracteres especiales XML
  - Formato numérico con 2 decimales

- ✅ **Envío al SRI:**
  - Web Service de Recepción de Comprobantes
  - SOAP/XML request
  - Manejo de respuestas (RECIBIDA/DEVUELTA)

- ✅ **Consulta de Autorización:**
  - Web Service de Autorización de Comprobantes
  - Polling automático cada 3 segundos
  - Máximo 10 intentos (30 segundos total)
  - Estados: AUTORIZADO/NO AUTORIZADO/PENDIENTE

- ✅ **Proceso Completo:**
  - `procesarFactura()`: Método unificado que:
    1. Genera clave de acceso
    2. Construye XML del comprobante
    3. Envía al SRI
    4. Espera y consulta autorización
    5. Retorna resultado completo

- ✅ **Mapeo de Formas de Pago:**
  - Efectivo → 01
  - Tarjeta → 19
  - Transferencia → 20

### 4. **Datos de Configuración**
- ✅ Información del contribuyente (configurable):
  - Razón Social: "CONSULTORIO DENTAL DR. CARLOS MENDOZA"
  - Nombre Comercial: "MEDICSYS Dental"
  - RUC: 0999999999001
  - Dirección Matriz: "Av. Principal 123 y Secundaria, Cuenca - Ecuador"
  - Obligado a llevar contabilidad: SÍ
  - Contribuyente especial: (opcional)

### 5. **Integración con el Sistema**
- ✅ Rutas configuradas:
  - `/odontologo/facturacion` → Listado
  - `/odontologo/facturacion/new` → Nueva factura
- ✅ Guard de autenticación aplicado
- ✅ Navegación desde dashboard (acción rápida "Nueva Factura")
- ✅ Diseño consistente con el resto del sistema
- ✅ Responsive design
- ✅ Animaciones y transiciones

---

## 📋 Estructura de Archivos

```
MEDICSYS.Web/src/app/
├── pages/odontologo/
│   ├── odontologo-facturacion/
│   │   ├── odontologo-facturacion.ts          # Componente listado
│   │   ├── odontologo-facturacion.html        # Template listado
│   │   └── odontologo-facturacion.scss        # Estilos listado
│   └── odontologo-factura-form/
│       ├── odontologo-factura-form.ts         # Componente formulario
│       ├── odontologo-factura-form.html       # Template formulario
│       └── odontologo-factura-form.scss       # Estilos formulario
└── core/
    └── sri.service.ts                          # Servicio integración SRI
```

---

## 🔐 Seguridad y Certificados Digitales

### ⚠️ IMPORTANTE: Firma Digital

**La implementación actual NO incluye la firma digital del XML**, que es **OBLIGATORIA** para el ambiente de producción del SRI.

### Requisitos para Producción:

1. **Certificado Digital (.p12)**
   - Obtener certificado de firma electrónica de:
     - Banco Central del Ecuador
     - Security Data
     - Anfac Ecuador
   - El certificado debe estar a nombre del RUC del contribuyente

2. **Implementación de Firma:**
   ```typescript
   // Se debe implementar la firma digital usando:
   - Librería: xml-crypto o similar
   - Algoritmo: RSA-SHA1
   - Formato: XML Signature (xmldsig)
   ```

3. **Backend Recomendado:**
   ```
   Por seguridad, la firma digital debería hacerse en el BACKEND:
   
   .NET Core → Usar BouncyCastle o System.Security.Cryptography
   - Cargar certificado .p12 con contraseña
   - Firmar XML antes de enviar al SRI
   - Retornar XML firmado al frontend
   ```

### Flujo de Producción Completo:
```
Frontend                    Backend                     SRI
   |                           |                          |
   |--[Datos Factura]--------->|                          |
   |                           |--[Genera XML]            |
   |                           |--[Firma con .p12]        |
   |                           |--[Envía comprobante]---->|
   |                           |                          |--[Valida]
   |                           |                          |--[RECIBIDA]
   |                           |<-------------------------|
   |                           |                          |
   |                           |--[Consulta autorización]>|
   |                           |                          |--[AUTORIZADO + #]
   |                           |<-------------------------|
   |<--[Resultado]-------------|                          |
```

---

## 🛠️ Uso del Sistema

### Crear Nueva Factura:

1. Ir a Facturación → "Nueva Factura"
2. Seleccionar cliente:
   - Consumidor Final (para montos < $200)
   - Cliente frecuente (un clic)
   - Nuevo cliente (llenar formulario)
3. Agregar items/servicios:
   - Usar tags de servicios predefinidos O
   - Escribir descripción manual
   - Ajustar cantidad, precio, descuento
4. Seleccionar forma de pago
5. Revisar totales
6. Click "Guardar y Enviar al SRI"
7. El sistema automáticamente:
   - Genera la clave de acceso
   - Construye el XML
   - Envía al SRI
   - Espera autorización
   - Muestra resultado

### Consultar Facturas:

1. Ir a Facturación
2. Ver métricas generales
3. Filtrar por estado
4. Ver detalles de cualquier factura
5. Descargar PDF
6. Reenviar al SRI si falló

---

## 📊 Backend Pendiente

### Modelos C# (Models/):

```csharp
public class Factura
{
    public int Id { get; set; }
    public string Numero { get; set; }
    public DateTime Fecha { get; set; }
    public string ClaveAcceso { get; set; }
    public string AutorizacionSRI { get; set; }
    public EstadoFacturaSRI Estado { get; set; }
    
    // Cliente
    public string ClienteTipoIdentificacion { get; set; }
    public string ClienteIdentificacion { get; set; }
    public string ClienteNombre { get; set; }
    public string ClienteEmail { get; set; }
    
    // Totales
    public decimal Subtotal { get; set; }
    public decimal IVA { get; set; }
    public decimal Total { get; set; }
    
    public string FormaPago { get; set; }
    public string Observaciones { get; set; }
    
    public List<FacturaItem> Items { get; set; }
}

public class FacturaItem
{
    public int Id { get; set; }
    public int FacturaId { get; set; }
    public string Codigo { get; set; }
    public string Descripcion { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Subtotal { get; set; }
}

public enum EstadoFacturaSRI
{
    Pendiente,
    Enviada,
    Autorizada,
    Rechazada
}
```

### Controller (Controllers/FacturasController.cs):

```csharp
[ApiController]
[Route("api/[controller]")]
public class FacturasController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetFacturas([FromQuery] string? estado)
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFactura(int id)
    
    [HttpPost]
    public async Task<IActionResult> CreateFactura([FromBody] FacturaRequest request)
    // 1. Guardar en BD
    // 2. Firmar XML con certificado .p12
    // 3. Enviar al SRI
    // 4. Actualizar estado
    // 5. Enviar email al cliente
    
    [HttpPost("{id}/reenviar")]
    public async Task<IActionResult> ReenviarSRI(int id)
    
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> DescargarPDF(int id)
}
```

### Servicios Backend:

1. **SRIService.cs**: Lógica de integración SRI con firma digital
2. **EmailService.cs**: Envío de facturas por email
3. **PDFService.cs**: Generación de RIDE (PDF de la factura)

---

## 🎨 Diseño UI/UX

- ✅ Diseño moderno con cards y métricas visuales
- ✅ Iconografía SVG inline consistente
- ✅ Palette de colores:
  - Verde (#22c55e): Autorizadas
  - Naranja (#fb923c): Pendientes
  - Rojo (#ef4444): Rechazadas
  - Púrpura (var(--primary)): Totales
- ✅ Typography: Inter + Sora
- ✅ Animaciones suaves en hover
- ✅ Responsive grid layout
- ✅ Feedback visual en todos los estados

---

## 🚀 Próximos Pasos

### Inmediatos:
1. Implementar backend en .NET Core
2. Obtener certificado digital .p12
3. Implementar firma digital en backend
4. Crear endpoints API
5. Conectar frontend con API real
6. Implementar generación de PDF (RIDE)
7. Implementar envío de emails

### Mejoras Futuras:
1. Notas de crédito
2. Retenciones
3. Guías de remisión
4. Comprobantes de retención
5. Reportes y estadísticas
6. Reconciliación bancaria
7. Recordatorios de pago

---

## 📝 Notas Importantes

### SRI Ecuador:
- IVA actual: 15% (puede cambiar según legislación)
- Secuencial debe ser correlativo
- Clave de acceso debe ser única
- Ambiente de pruebas acepta RUC de prueba
- Producción requiere RUC real y certificado válido

### Testing:
- Usar ambiente de pruebas inicialmente
- RUC de prueba: 0999999999001
- No usar datos reales en pruebas
- Validar XML contra XSD del SRI antes de enviar

### Seguridad:
- ⚠️ NUNCA exponer certificados .p12 en frontend
- ⚠️ Toda firma digital debe ser en backend
- ⚠️ Encriptar certificados en servidor
- ⚠️ Usar HTTPS en producción

---

## ✅ Estado del Módulo

**FRONTEND: 100% COMPLETO**
- Todas las pantallas implementadas
- Servicio SRI implementado (sin firma digital)
- Rutas configuradas
- Diseño finalizado

**BACKEND: 0% PENDIENTE**
- Modelos por crear
- Controllers por implementar
- Firma digital por implementar
- Integración con BD por hacer

**INTEGRACIÓN SRI: 70% COMPLETO**
- Generación clave de acceso ✅
- Construcción XML ✅
- Envío al SRI ✅
- Consulta autorización ✅
- Firma digital ❌ (CRÍTICO)

---

## 📞 Soporte

Para implementación de firma digital y certificados, consultar:
- [Documentación oficial SRI](https://www.sri.gob.ec/facturacion-electronica)
- [Esquemas XSD del SRI](https://www.sri.gob.ec/esquemas-xsd)
- Proveedores de certificados digitales autorizados en Ecuador

---

**Desarrollado para MEDICSYS - Sistema de Gestión Odontológica**
