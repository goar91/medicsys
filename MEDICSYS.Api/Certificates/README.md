# Certificados de Firma Electrónica - SRI Ecuador

## ⚠️ IMPORTANTE - SEGURIDAD

Esta carpeta está diseñada para almacenar el **certificado digital .p12** necesario para firmar comprobantes electrónicos del SRI. Los certificados digitales contienen claves privadas y **NUNCA** deben ser compartidos o subidos a repositorios de código.

### 🔒 Medidas de Seguridad Implementadas

1. **.gitignore configurado**: Todos los archivos `.p12`, `.pfx`, `.key` y `.pem` están ignorados por Git
2. **Configuración separada**: Las contraseñas se almacenan en archivos de configuración también ignorados
3. **Solo local**: Los certificados solo deben existir en el servidor de producción

---

## 📋 Cómo Obtener el Certificado Digital

### Proveedores Autorizados en Ecuador:

1. **Banco Central del Ecuador**
   - Web: https://www.eci.bce.ec/
   - Certificado: Persona Natural o Jurídica
   - Vigencia: 2 años

2. **Security Data**
   - Web: https://www.securitydata.net.ec/
   - Certificado: Firma Electrónica
   - Vigencia: 1-2 años

3. **ANF AC Ecuador**
   - Web: https://www.anf.ec/
   - Certificado: Firma Electrónica Calificada
   - Vigencia: 2 años

### Requisitos:
- Cédula de identidad o RUC (debe coincidir con el RUC del contribuyente)
- Correo electrónico
- Costo aproximado: $60 - $150 USD
- Formato: `.p12` (también llamado PKCS#12)

---

## 📁 Estructura de Archivos

```
Certificates/
├── .gitignore                    # Protección de seguridad
├── README.md                     # Este archivo
├── sri-config.example.json       # Plantilla de configuración (SIN datos reales)
├── sri-config.json               # Tu configuración REAL (ignorado por Git)
└── [tu-certificado].p12          # Tu certificado (ignorado por Git)
```

---

## ⚙️ Configuración

### Paso 1: Copiar el Certificado

Coloca tu archivo `.p12` en esta carpeta:

```bash
# Ejemplo:
MEDICSYS.Api/Certificates/mi-empresa-2026.p12
```

### Paso 2: Crear Configuración

Copia el archivo de ejemplo y renómbralo:

```bash
cp sri-config.example.json sri-config.json
```

### Paso 3: Editar sri-config.json

Completa con tus datos reales:

```json
{
  "SRI": {
    "Ambiente": "pruebas",
    "RUC": "0999999999001",
    "RazonSocial": "CONSULTORIO DENTAL DR. CARLOS MENDOZA",
    "NombreComercial": "MEDICSYS Dental",
    "DireccionMatriz": "Av. Principal 123 y Secundaria, Cuenca - Ecuador",
    "ObligadoContabilidad": "SI",
    "ContribuyenteEspecial": "",
    "Establecimiento": "001",
    "PuntoEmision": "001",
    "Certificado": {
      "Archivo": "mi-empresa-2026.p12",
      "Clave": "MI_CONTRASEÑA_SUPER_SECRETA"
    }
  }
}
```

### Paso 4: Configurar appsettings.json

En `appsettings.Production.json`, agrega la ruta:

```json
{
  "SRI": {
    "ConfigPath": "Certificates/sri-config.json"
  }
}
```

---

## 🔧 Uso en el Código

### Cargar el Certificado en C#

```csharp
using System.Security.Cryptography.X509Certificates;

public class SRIService
{
    private X509Certificate2 LoadCertificate(string certPath, string password)
    {
        var fullPath = Path.Combine(
            Directory.GetCurrentDirectory(), 
            "Certificates", 
            certPath
        );
        
        return new X509Certificate2(
            fullPath, 
            password, 
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet
        );
    }
    
    public void FirmarXML(string xml, string certPath, string certPassword)
    {
        var certificate = LoadCertificate(certPath, certPassword);
        
        // Firmar XML con BouncyCastle o System.Security.Cryptography.Xml
        // ...
    }
}
```

---

## 🚀 Ambientes

### Ambiente de Pruebas
- URL Recepción: `https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline`
- URL Autorización: `https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline`
- Certificado: Puede ser uno de pruebas o producción
- RUC: Cualquier RUC válido (puede ser ficticio)

### Ambiente de Producción
- URL Recepción: `https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline`
- URL Autorización: `https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline`
- Certificado: **DEBE** ser real y válido
- RUC: **DEBE** coincidir con el certificado

---

## ✅ Checklist de Seguridad

Antes de pasar a producción, verifica:

- [ ] El archivo `.p12` existe en esta carpeta
- [ ] El archivo `sri-config.json` tiene los datos correctos
- [ ] La contraseña del certificado es correcta
- [ ] El RUC del certificado coincide con el RUC en la configuración
- [ ] El certificado no ha expirado (verificar vigencia)
- [ ] `.gitignore` está protegiendo los archivos sensibles
- [ ] Los backups del certificado están en lugar seguro (fuera del servidor)
- [ ] Solo personal autorizado tiene acceso a esta carpeta en producción

---

## 🔄 Renovación del Certificado

Los certificados digitales tienen vigencia limitada (1-2 años). Cuando venza:

1. Obtener nuevo certificado del proveedor
2. Reemplazar el archivo `.p12` en esta carpeta
3. Actualizar `sri-config.json` con el nuevo nombre de archivo
4. Actualizar la contraseña si cambió
5. Reiniciar la aplicación
6. Verificar con una factura de prueba

---

## 📞 Soporte

### Soporte Técnico SRI:
- Teléfono: 1700 774 774
- Web: https://www.sri.gob.ec
- Email: atcliente@sri.gob.ec

### Documentación Oficial:
- Facturación Electrónica: https://www.sri.gob.ec/facturacion-electronica
- Esquemas XSD: https://www.sri.gob.ec/esquemas-xsd
- Ficha Técnica: Descargar desde el portal del SRI

---

## ⚠️ Problemas Comunes

### Error: "Certificado no válido"
- Verificar que el certificado no haya expirado
- Verificar que la contraseña sea correcta
- Verificar que el RUC del certificado coincida

### Error: "Firma no válida"
- Verificar el algoritmo de firma (debe ser SHA1 o SHA256)
- Verificar el formato del XML firmado
- Usar un validador de XML antes de enviar al SRI

### Error: "Acceso denegado al archivo .p12"
- Verificar permisos de lectura en el archivo
- En Windows: Clic derecho → Propiedades → Seguridad
- En Linux: `chmod 600 mi-certificado.p12`

---

**Última actualización**: Febrero 2026  
**Sistema**: MEDICSYS - Gestión Odontológica
