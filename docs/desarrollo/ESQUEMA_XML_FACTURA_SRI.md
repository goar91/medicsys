# Esquema XML de Factura SRI (Implementado)

La generacion del XML de factura se realiza en `MEDICSYS.Api/Services/SriService.cs` con esta estructura:

- Nodo raiz: `<factura id="comprobante" version="1.0.0">`
- Secciones principales:
  - `infoTributaria`
  - `infoFactura`
  - `detalles`
  - `infoAdicional` (opcional)

## Flujo de documentos

Los documentos se guardan automaticamente en `MEDICSYS.Api/storage/facturacion`:

- `Doc Generados`: XML base del comprobante.
- `Doc Firmados`: XML firmado (actualmente mock, mismo contenido del generado).
- `Doc Respuestas`: XML de respuesta de recepcion/autorizacion.
- `Doc Autorizados`: XML de autorizacion cuando el estado es `AUTORIZADO`.

## Campos clave incluidos en el XML

- `infoTributaria`:
  - `ambiente` (1 pruebas, 2 produccion)
  - `tipoEmision`
  - `razonSocial`
  - `nombreComercial`
  - `ruc`
  - `claveAcceso`
  - `codDoc`
  - `estab`
  - `ptoEmi`
  - `secuencial`
  - `dirMatriz`
- `infoFactura`:
  - `fechaEmision`
  - `dirEstablecimiento`
  - `contribuyenteEspecial` (si aplica)
  - `obligadoContabilidad`
  - `tipoIdentificacionComprador`
  - `razonSocialComprador`
  - `identificacionComprador`
  - `direccionComprador`
  - `totalSinImpuestos`
  - `totalDescuento`
  - `totalConImpuestos`
  - `importeTotal`
  - `pagos`
- `detalles`:
  - `detalle/codigoPrincipal`
  - `detalle/descripcion`
  - `detalle/cantidad`
  - `detalle/precioUnitario`
  - `detalle/descuento`
  - `detalle/precioTotalSinImpuesto`
  - `detalle/impuestos/impuesto`

## Nota

Para ambiente de produccion, falta integrar firma electronica real con certificado `.p12`.
