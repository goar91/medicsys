using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Options;
using MEDICSYS.Api.Models;

namespace MEDICSYS.Api.Services;

public record SriSendResult(
    string Status,
    string AccessKey,
    string? AuthorizationNumber,
    DateTime? AuthorizedAt,
    string? Messages,
    string? GeneratedXmlPath = null,
    string? SignedXmlPath = null,
    string? ResponseXmlPath = null,
    string? AuthorizedXmlPath = null);

public record SriAuthorizationQueryResult(
    string Status,
    string? AuthorizationNumber,
    DateTime? AuthorizedAt,
    string? Messages);

public interface ISriService
{
    Task<SriSendResult> SendInvoiceAsync(Invoice invoice, string? environment = null);

    /// <summary>
    /// Consulta directamente el estado de autorización en el SRI para una clave de acceso.
    /// </summary>
    Task<SriAuthorizationQueryResult> QueryAuthorizationAsync(string accessKey, string? environment = null);
}

public class SriOptions
{
    public bool Mock { get; set; } = false;
    public string Ambiente { get; set; } = "Pruebas"; // Pruebas | Produccion
    public string Ruc { get; set; } = "0999999999001";
    public string RazonSocial { get; set; } = "CONSULTORIO DENTAL DR. CARLOS MENDOZA";
    public string NombreComercial { get; set; } = "MEDICSYS Dental";
    public string DireccionMatriz { get; set; } = "Av. Principal 123 y Secundaria, Cuenca - Ecuador";
    public string Establecimiento { get; set; } = "001";
    public string PuntoEmision { get; set; } = "001";
    public string RutaDocumentos { get; set; } = Path.Combine("storage", "facturacion");
    public string CarpetaDocGenerados { get; set; } = "Doc Generados";
    public string CarpetaDocAutorizados { get; set; } = "Doc Autorizados";
    public string CarpetaDocFirmados { get; set; } = "Doc Firmados";
    public string CarpetaDocRespuestas { get; set; } = "Doc Respuestas";
    public string ObligadoContabilidad { get; set; } = "SI";
    public string? ContribuyenteEspecial { get; set; }
    public string? DireccionEstablecimiento { get; set; }
    public string? CertificadoArchivo { get; set; }
    public string? CertificadoClave { get; set; }
}

public class SriService : ISriService
{
    private const decimal VatRate = 15m;
    private const string AwaitingAuthorizationStatus = "EN_ESPERA_AUTORIZACION";
    private static readonly TimeSpan AuthorizationInitialDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan AuthorizationRetryInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan AuthorizationMaxWait = TimeSpan.FromSeconds(30);

    // Endpoints oficiales del SRI de Ecuador
    private const string SriRecepcionPruebas =
        "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline";
    private const string SriAutorizacionPruebas =
        "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline";
    private const string SriRecepcionProduccion =
        "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline";
    private const string SriAutorizacionProduccion =
        "https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline";

    private readonly SriOptions _options;
    private readonly ILogger<SriService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _docGeneradosDir;
    private readonly string _docAutorizadosDir;
    private readonly string _docFirmadosDir;
    private readonly string _docRespuestasDir;

    public SriService(IOptions<SriOptions> options, ILogger<SriService> logger, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;

        var root = ResolveDirectory(_options.RutaDocumentos);
        _docGeneradosDir = EnsureDirectory(Path.Combine(root, _options.CarpetaDocGenerados));
        _docAutorizadosDir = EnsureDirectory(Path.Combine(root, _options.CarpetaDocAutorizados));
        _docFirmadosDir = EnsureDirectory(Path.Combine(root, _options.CarpetaDocFirmados));
        _docRespuestasDir = EnsureDirectory(Path.Combine(root, _options.CarpetaDocRespuestas));
    }

    public async Task<SriSendResult> SendInvoiceAsync(Invoice invoice, string? environment = null)
    {
        var environmentValue = NormalizeEnvironment(environment ?? invoice.SriEnvironment);
        var accessKey = GenerateAccessKey(invoice.IssuedAt, invoice.Sequential, environmentValue);
        var xml = BuildInvoiceXml(invoice, accessKey, environmentValue);
        var baseName = BuildBaseFileName(invoice.Number, accessKey);

        var generatedXmlPath = WriteDocument(_docGeneradosDir, $"{baseName}.xml", xml);

        if (_options.Mock)
        {
            var signedXmlFallback = xml; // Sin firma digital en modo de pruebas
            var signedXmlFallbackPath = WriteDocument(_docFirmadosDir, $"{baseName}.xml", signedXmlFallback);

            var authorized = invoice.Total > 0;
            var status = authorized ? "AUTORIZADO" : "RECHAZADO";
            var authorization = authorized ? accessKey : null;
            var message = authorized ? null : "Monto inválido para autorización.";
            var authorizedAt = authorized ? DateTimeHelper.Now() : (DateTime?)null;

            var responseXml = BuildReceptionResponseXml(status, accessKey, message);
            var responseXmlPath = WriteDocument(_docRespuestasDir, $"{baseName}.xml", responseXml);

            string? authorizedXmlPath = null;
            if (authorized && authorization != null && authorizedAt.HasValue)
            {
                var authorizationXml = BuildAuthorizationXml(authorization, authorizedAt.Value, environmentValue, signedXmlFallback);
                authorizedXmlPath = WriteDocument(_docAutorizadosDir, $"{baseName}.xml", authorizationXml);
            }

            _logger.LogInformation("SRI MOCK {Environment}: {Status} para factura {Number}", environmentValue, status, invoice.Number);
            return new SriSendResult(status, accessKey, authorization, authorizedAt, message,
                generatedXmlPath, signedXmlFallbackPath, responseXmlPath, authorizedXmlPath);
        }

        // ═══════════════════════════════════════════════════════════
        // FLUJO REAL: Firma digital + Envío al SRI por Web Services
        // ═══════════════════════════════════════════════════════════
        try
        {
            // 1. Firmar XML con certificado .p12 (XAdES-BES)
            _logger.LogInformation("Firmando XML de factura {Number} con certificado digital XAdES-BES...", invoice.Number);
            var certPath = ResolveCertificatePath();
            var signedXml = SriXadesSigner.SignXml(xml, certPath, _options.CertificadoClave!);
            var signedXmlPath = WriteDocument(_docFirmadosDir, $"{baseName}.xml", signedXml);
            _logger.LogInformation("XML firmado exitosamente para factura {Number}", invoice.Number);

            // 2. Enviar al SRI (Recepción)
            _logger.LogInformation("Enviando comprobante al SRI ({Environment})...", environmentValue);
            var (receptionStatus, receptionMessages) = await SendToSriReceptionAsync(signedXml, environmentValue);
            _logger.LogInformation("Respuesta recepción SRI: {Status} - {Messages}",
                receptionStatus, receptionMessages ?? "Sin mensajes");

            if (string.Equals(receptionStatus, "DEVUELTA", StringComparison.OrdinalIgnoreCase))
            {
                // Caso especial [70]: la clave ya está en procesamiento en el SRI
                // (posible re-envío o estado pendiente de una transmisión anterior).
                // En lugar de rechazar, consultamos la autorización directamente.
                var isKeyInProcessing = receptionMessages != null
                    && (receptionMessages.Contains("[70]", StringComparison.OrdinalIgnoreCase)
                        || receptionMessages.Contains("EN PROCESAMIENTO", StringComparison.OrdinalIgnoreCase));

                if (!isKeyInProcessing)
                {
                    var responseXml = BuildReceptionResponseXml("DEVUELTA", accessKey, receptionMessages);
                    var responseXmlPath = WriteDocument(_docRespuestasDir, $"{baseName}.xml", responseXml);
                    return new SriSendResult("RECHAZADO", accessKey, null, null,
                        $"Comprobante devuelto por el SRI: {receptionMessages}",
                        generatedXmlPath, signedXmlPath, responseXmlPath, null);
                }

                _logger.LogInformation(
                    "Comprobante con clave {AccessKey} ya estaba en procesamiento en el SRI. " +
                    "Procediendo a consultar autorización...", accessKey);
            }

            // 3. Consultar Autorización (espera máxima de 30 segundos)
            _logger.LogInformation(
                "Comprobante recibido por SRI. Consultando autorización para clave {AccessKey} (máximo {Seconds}s)...",
                accessKey,
                AuthorizationMaxWait.TotalSeconds);

            var startedAt = DateTime.UtcNow;
            await Task.Delay(AuthorizationInitialDelay);

            var retry = 0;
            while (DateTime.UtcNow - startedAt < AuthorizationMaxWait)
            {
                retry++;
                var authResult = await QuerySriAuthorizationAsync(accessKey, environmentValue);
                _logger.LogInformation("Consulta autorización intento {Retry}: {Status}", retry, authResult.Status);

                if (string.Equals(authResult.Status, "AUTORIZADO", StringComparison.OrdinalIgnoreCase))
                {
                    var authXml = authResult.Comprobante
                        ?? BuildAuthorizationXml(authResult.AuthNumber!, authResult.AuthorizedAt!.Value, environmentValue, signedXml);
                    var authorizedXmlPath = WriteDocument(_docAutorizadosDir, $"{baseName}.xml", authXml);
                    var okResponseXml = BuildReceptionResponseXml("AUTORIZADO", accessKey, null);
                    var okResponseXmlPath = WriteDocument(_docRespuestasDir, $"{baseName}.xml", okResponseXml);

                    _logger.LogInformation(
                        "Factura {Number} AUTORIZADA por el SRI. Autorización: {Auth}",
                        invoice.Number, authResult.AuthNumber);

                    return new SriSendResult("AUTORIZADO", accessKey, authResult.AuthNumber,
                        authResult.AuthorizedAt, null,
                        generatedXmlPath, signedXmlPath, okResponseXmlPath, authorizedXmlPath);
                }

                if (string.Equals(authResult.Status, "NO AUTORIZADO", StringComparison.OrdinalIgnoreCase))
                {
                    var noAuthXml = BuildReceptionResponseXml("NO AUTORIZADO", accessKey, authResult.Messages);
                    var noAuthXmlPath = WriteDocument(_docRespuestasDir, $"{baseName}.xml", noAuthXml);

                    _logger.LogWarning("Factura {Number} NO AUTORIZADA: {Messages}", invoice.Number, authResult.Messages);

                    return new SriSendResult("NO AUTORIZADO", accessKey, null, null,
                        $"No autorizado por el SRI: {authResult.Messages}",
                        generatedXmlPath, signedXmlPath, noAuthXmlPath, null);
                }

                var elapsed = DateTime.UtcNow - startedAt;
                var remaining = AuthorizationMaxWait - elapsed;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                var nextDelay = remaining < AuthorizationRetryInterval
                    ? remaining
                    : AuthorizationRetryInterval;

                _logger.LogInformation(
                    "Comprobante aún EN PROCESO. Reintentando en {Seconds} segundo(s)...",
                    Math.Max(1, (int)Math.Ceiling(nextDelay.TotalSeconds)));

                await Task.Delay(nextDelay);
            }

            var timeoutMessage =
                $"El SRI no respondió con un estado definitivo en {AuthorizationMaxWait.TotalSeconds:0} segundos. " +
                "El documento quedó en espera de autorización para reintento manual.";

            _logger.LogWarning(
                "Factura {Number}: {Message}",
                invoice.Number,
                timeoutMessage);

            var pendingXml = BuildReceptionResponseXml("EN ESPERA AUTORIZACION", accessKey, timeoutMessage);
            var pendingPath = WriteDocument(_docRespuestasDir, $"{baseName}.xml", pendingXml);
            return new SriSendResult(AwaitingAuthorizationStatus, accessKey, null, null, timeoutMessage,
                generatedXmlPath, signedXmlPath, pendingPath, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar factura {Number} con el SRI: {Message}", invoice.Number, ex.Message);
            var errorXml = BuildReceptionResponseXml("ERROR", accessKey, ex.Message);
            var errorPath = WriteDocument(_docRespuestasDir, $"{baseName}.xml", errorXml);
            return new SriSendResult("ERROR", accessKey, null, null,
                $"Error al comunicarse con el SRI: {ex.Message}",
                generatedXmlPath, null, errorPath, null);
        }
    }

    private static string NormalizeEnvironment(string? environment)
    {
        if (string.Equals(environment, "Produccion", StringComparison.OrdinalIgnoreCase))
        {
            return "Produccion";
        }

        return "Pruebas";
    }

    private string GenerateAccessKey(DateTime date, int sequential, string environment)
    {
        // La fecha ya viene en hora Ecuador desde DateTimeHelper.Now()
        var dia = date.Day.ToString().PadLeft(2, '0');
        var mes = date.Month.ToString().PadLeft(2, '0');
        var anio = date.Year.ToString();
        var tipoComprobante = "01";
        var ambiente = environment.Equals("Produccion", StringComparison.OrdinalIgnoreCase) ? "2" : "1";
        var establecimiento = _options.Establecimiento.PadLeft(3, '0');
        var puntoEmision = _options.PuntoEmision.PadLeft(3, '0');
        var secuencial = sequential.ToString().PadLeft(9, '0');
        var codigoNumerico = RandomNumberGenerator.GetInt32(10000000, 99999999).ToString();
        var tipoEmision = "1";

        var claveBase = string.Concat(dia, mes, anio, tipoComprobante, _options.Ruc, ambiente, establecimiento, puntoEmision, secuencial, codigoNumerico, tipoEmision);
        var digito = CalcularModulo11(claveBase);
        return claveBase + digito;
    }

    private static string CalcularModulo11(string cadena)
    {
        var factor = 2;
        var suma = 0;

        for (var i = cadena.Length - 1; i >= 0; i--)
        {
            suma += (cadena[i] - '0') * factor;
            factor = factor == 7 ? 2 : factor + 1;
        }

        var residuo = suma % 11;
        var resultado = 11 - residuo;

        if (resultado == 11)
        {
            return "0";
        }

        if (resultado == 10)
        {
            return "1";
        }

        return resultado.ToString(CultureInfo.InvariantCulture);
    }

    // Nota: En el SRI real, el numero de autorizacion es la misma clave de acceso (49 digitos).
    // Este metodo se mantiene por compatibilidad historica.
    private static string GenerateAuthorizationNumber(string accessKey)
    {
        return accessKey;
    }

    private string BuildInvoiceXml(Invoice invoice, string accessKey, string environment)
    {
        var ambiente = environment.Equals("Produccion", StringComparison.OrdinalIgnoreCase) ? "2" : "1";
        var estab = _options.Establecimiento.PadLeft(3, '0');
        var ptoEmi = _options.PuntoEmision.PadLeft(3, '0');
        var secuencial = invoice.Sequential.ToString().PadLeft(9, '0');
        var paymentCode = MapPaymentMethod(invoice.PaymentMethod);
        var fechaEmision = invoice.IssuedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var dirEstablecimiento = _options.DireccionEstablecimiento ?? _options.DireccionMatriz;

        var details = new StringBuilder();
        for (var i = 0; i < invoice.Items.Count; i++)
        {
            var item = invoice.Items[i];
            var codigo = (i + 1).ToString("000", CultureInfo.InvariantCulture);
            var taxPercentCode = item.Tax > 0 ? "4" : "0";
            var taxRate = item.Tax > 0 ? VatRate : 0m;

            details.AppendLine("    <detalle>");
            details.AppendLine($"      <codigoPrincipal>{codigo}</codigoPrincipal>");
            details.AppendLine($"      <descripcion>{EscapeXml(item.Description)}</descripcion>");
            details.AppendLine($"      <cantidad>{item.Quantity.ToString(CultureInfo.InvariantCulture)}</cantidad>");
            details.AppendLine($"      <precioUnitario>{FormatDecimal(item.UnitPrice)}</precioUnitario>");
            details.AppendLine($"      <descuento>{FormatDecimal(item.Quantity * item.UnitPrice * (item.DiscountPercent / 100m))}</descuento>");
            details.AppendLine($"      <precioTotalSinImpuesto>{FormatDecimal(item.Subtotal)}</precioTotalSinImpuesto>");
            details.AppendLine("      <impuestos>");
            details.AppendLine("        <impuesto>");
            details.AppendLine("          <codigo>2</codigo>");
            details.AppendLine($"          <codigoPorcentaje>{taxPercentCode}</codigoPorcentaje>");
            details.AppendLine($"          <tarifa>{FormatDecimal(taxRate)}</tarifa>");
            details.AppendLine($"          <baseImponible>{FormatDecimal(item.Subtotal)}</baseImponible>");
            details.AppendLine($"          <valor>{FormatDecimal(item.Tax)}</valor>");
            details.AppendLine("        </impuesto>");
            details.AppendLine("      </impuestos>");
            details.AppendLine("    </detalle>");
        }

        var additionalInfo = BuildAdditionalInfoXml(invoice);
        var contribuyenteEspecialXml = string.IsNullOrWhiteSpace(_options.ContribuyenteEspecial)
            ? string.Empty
            : $"    <contribuyenteEspecial>{EscapeXml(_options.ContribuyenteEspecial!)}</contribuyenteEspecial>{Environment.NewLine}";

        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<factura id=""comprobante"" version=""1.0.0"">
  <infoTributaria>
    <ambiente>{ambiente}</ambiente>
    <tipoEmision>1</tipoEmision>
    <razonSocial>{EscapeXml(_options.RazonSocial)}</razonSocial>
    <nombreComercial>{EscapeXml(_options.NombreComercial)}</nombreComercial>
    <ruc>{EscapeXml(_options.Ruc)}</ruc>
    <claveAcceso>{accessKey}</claveAcceso>
    <codDoc>01</codDoc>
    <estab>{estab}</estab>
    <ptoEmi>{ptoEmi}</ptoEmi>
    <secuencial>{secuencial}</secuencial>
    <dirMatriz>{EscapeXml(_options.DireccionMatriz)}</dirMatriz>
  </infoTributaria>
  <infoFactura>
    <fechaEmision>{fechaEmision}</fechaEmision>
    <dirEstablecimiento>{EscapeXml(dirEstablecimiento)}</dirEstablecimiento>
{contribuyenteEspecialXml}    <obligadoContabilidad>{NormalizeObligadoContabilidad(_options.ObligadoContabilidad)}</obligadoContabilidad>
    <tipoIdentificacionComprador>{EscapeXml(invoice.CustomerIdentificationType)}</tipoIdentificacionComprador>
    <razonSocialComprador>{EscapeXml(invoice.CustomerName)}</razonSocialComprador>
    <identificacionComprador>{EscapeXml(invoice.CustomerIdentification)}</identificacionComprador>
    <direccionComprador>{EscapeXml(string.IsNullOrWhiteSpace(invoice.CustomerAddress) ? "S/N" : invoice.CustomerAddress)}</direccionComprador>
    <totalSinImpuestos>{FormatDecimal(invoice.Subtotal)}</totalSinImpuestos>
    <totalDescuento>{FormatDecimal(invoice.DiscountTotal)}</totalDescuento>
    <totalConImpuestos>
      <totalImpuesto>
        <codigo>2</codigo>
        <codigoPorcentaje>{(invoice.Tax > 0 ? "4" : "0")}</codigoPorcentaje>
        <baseImponible>{FormatDecimal(invoice.Subtotal)}</baseImponible>
        <valor>{FormatDecimal(invoice.Tax)}</valor>
      </totalImpuesto>
    </totalConImpuestos>
    <propina>0.00</propina>
    <importeTotal>{FormatDecimal(invoice.Total)}</importeTotal>
    <moneda>DOLAR</moneda>
    <pagos>
      <pago>
        <formaPago>{paymentCode}</formaPago>
        <total>{FormatDecimal(invoice.TotalToCharge)}</total>
      </pago>
    </pagos>
  </infoFactura>
  <detalles>
{details}
  </detalles>
{additionalInfo}</factura>";
    }

    // ═══════════════════════════════════════════════════════════
    // Web Services SOAP del SRI
    // ═══════════════════════════════════════════════════════════

    private string GetSriReceptionUrl(string environment) =>
        environment.Equals("Produccion", StringComparison.OrdinalIgnoreCase)
            ? SriRecepcionProduccion : SriRecepcionPruebas;

    private string GetSriAuthorizationUrl(string environment) =>
        environment.Equals("Produccion", StringComparison.OrdinalIgnoreCase)
            ? SriAutorizacionProduccion : SriAutorizacionPruebas;

    /// <summary>
    /// Envía el comprobante firmado al Web Service de Recepción del SRI.
    /// </summary>
    private async Task<(string Status, string? Messages)> SendToSriReceptionAsync(
        string signedXml, string environment)
    {
        var xmlBytes = Encoding.UTF8.GetBytes(signedXml);
        var xmlBase64 = Convert.ToBase64String(xmlBytes);

        var soapEnvelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:ec=""http://ec.gob.sri.ws.recepcion"">
  <soapenv:Header/>
  <soapenv:Body>
    <ec:validarComprobante>
      <xml>{xmlBase64}</xml>
    </ec:validarComprobante>
  </soapenv:Body>
</soapenv:Envelope>";

        var url = GetSriReceptionUrl(environment);
        var client = _httpClientFactory.CreateClient("SRI");
        var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

        _logger.LogDebug("Enviando SOAP a {Url}", url);
        var response = await client.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogDebug("Respuesta SRI Recepción ({StatusCode}): {Body}", response.StatusCode, responseBody);

        // Parsear respuesta SOAP
        var responseDoc = new XmlDocument();
        responseDoc.LoadXml(responseBody);

        var estadoNode = responseDoc.SelectSingleNode("//*[local-name()='estado']");
        var status = estadoNode?.InnerText ?? "ERROR";

        var messages = ParseSriMessages(responseDoc);
        return (status, messages);
    }

    /// <summary>
    /// Consulta el Web Service de Autorización del SRI para obtener el estado de un comprobante.
    /// </summary>
    /// <summary>
    /// Implementación pública de consulta de autorización para uso por servicios externos (polling).
    /// </summary>
    public async Task<SriAuthorizationQueryResult> QueryAuthorizationAsync(string accessKey, string? environment = null)
    {
        var env = NormalizeEnvironment(environment);
        var (status, authNumber, authorizedAt, _, messages) = await QuerySriAuthorizationAsync(accessKey, env);
        return new SriAuthorizationQueryResult(status, authNumber, authorizedAt, messages);
    }

    private async Task<(string Status, string? AuthNumber, DateTime? AuthorizedAt, string? Comprobante, string? Messages)>
        QuerySriAuthorizationAsync(string accessKey, string environment)
    {
        var soapEnvelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:ec=""http://ec.gob.sri.ws.autorizacion"">
  <soapenv:Header/>
  <soapenv:Body>
    <ec:autorizacionComprobante>
      <claveAccesoComprobante>{accessKey}</claveAccesoComprobante>
    </ec:autorizacionComprobante>
  </soapenv:Body>
</soapenv:Envelope>";

        var url = GetSriAuthorizationUrl(environment);
        var client = _httpClientFactory.CreateClient("SRI");
        var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

        _logger.LogDebug("Consultando autorización en {Url} para clave {Key}", url, accessKey);
        var response = await client.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        _logger.LogDebug("Respuesta SRI Autorización ({StatusCode}): {Body}", response.StatusCode, responseBody);

        var responseDoc = new XmlDocument();
        responseDoc.LoadXml(responseBody);

        // Buscar el elemento <autorizacion>
        var authNode = responseDoc.SelectSingleNode("//*[local-name()='autorizacion']");
        if (authNode == null)
        {
            return ("EN PROCESO", null, null, null,
                "No se encontró elemento de autorización en la respuesta del SRI");
        }

        var estado = authNode.SelectSingleNode("*[local-name()='estado']")?.InnerText ?? "EN PROCESO";
        var numAutorizacion = authNode.SelectSingleNode("*[local-name()='numeroAutorizacion']")?.InnerText;
        var fechaAuth = authNode.SelectSingleNode("*[local-name()='fechaAutorizacion']")?.InnerText;
        var comprobante = authNode.SelectSingleNode("*[local-name()='comprobante']")?.InnerText;

        DateTime? authorizedAt = null;
        if (!string.IsNullOrEmpty(fechaAuth))
        {
            if (DateTime.TryParse(fechaAuth, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal, out var parsed))
            {
                authorizedAt = parsed;
            }
        }

        var messages = ParseSriMessages(responseDoc);
        return (estado, numAutorizacion, authorizedAt, comprobante, messages);
    }

    /// <summary>
    /// Extrae y formatea los mensajes de una respuesta XML del SRI.
    /// </summary>
    private static string? ParseSriMessages(XmlDocument doc)
    {
        // <mensajes><mensaje><identificador/><mensaje/><tipo/><informacionAdicional/></mensaje></mensajes>
        var mensajeContainers = doc.SelectNodes("//*[local-name()='mensajes']/*[local-name()='mensaje']");
        if (mensajeContainers == null || mensajeContainers.Count == 0) return null;

        var parts = new List<string>();
        foreach (XmlNode msgNode in mensajeContainers)
        {
            var id = msgNode.SelectSingleNode("*[local-name()='identificador']")?.InnerText;
            var msg = msgNode.SelectSingleNode("*[local-name()='mensaje']")?.InnerText;
            var info = msgNode.SelectSingleNode("*[local-name()='informacionAdicional']")?.InnerText;
            var tipo = msgNode.SelectSingleNode("*[local-name()='tipo']")?.InnerText;

            var entry = new List<string>();
            if (!string.IsNullOrEmpty(id)) entry.Add($"[{id}]");
            if (!string.IsNullOrEmpty(tipo)) entry.Add($"({tipo})");
            if (!string.IsNullOrEmpty(msg)) entry.Add(msg);
            if (!string.IsNullOrEmpty(info)) entry.Add(info);

            if (entry.Count > 0) parts.Add(string.Join(" ", entry));
        }

        return parts.Count > 0 ? string.Join(" | ", parts) : null;
    }

    /// <summary>
    /// Resuelve la ruta absoluta al certificado .p12.
    /// </summary>
    private string ResolveCertificatePath()
    {
        if (string.IsNullOrWhiteSpace(_options.CertificadoArchivo))
            throw new InvalidOperationException(
                "No se ha configurado la ruta del certificado digital (.p12). Configure 'Sri:CertificadoArchivo' en appsettings.json");

        if (string.IsNullOrWhiteSpace(_options.CertificadoClave))
            throw new InvalidOperationException(
                "No se ha configurado la contraseña del certificado digital. Configure 'Sri:CertificadoClave' en appsettings.json");

        var path = _options.CertificadoArchivo;
        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        }

        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"No se encontró el certificado digital en: {path}", path);

        return path;
    }

    private static string BuildReceptionResponseXml(string status, string accessKey, string? message)
    {
        var messagesXml = string.IsNullOrWhiteSpace(message)
            ? string.Empty
            : $@"
    <comprobante>
      <claveAcceso>{accessKey}</claveAcceso>
      <mensajes>
        <mensaje>
          <identificador>43</identificador>
          <mensaje>{EscapeXml(message)}</mensaje>
          <tipo>{(string.Equals(status, "RECHAZADO", StringComparison.OrdinalIgnoreCase) ? "ERROR" : "INFORMACION")}</tipo>
        </mensaje>
      </mensajes>
    </comprobante>";

        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<respuestaRecepcionComprobante>
  <estado>{status}</estado>{messagesXml}
</respuestaRecepcionComprobante>";
    }

    private static string BuildAuthorizationXml(string authorizationNumber, DateTime authorizedAt, string environment, string signedXml)
    {
        var ambiente = environment.Equals("Produccion", StringComparison.OrdinalIgnoreCase) ? "PRODUCCION" : "PRUEBAS";
        var comprobante = EscapeCData(signedXml);

        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<autorizacion>
  <estado>AUTORIZADO</estado>
  <numeroAutorizacion>{authorizationNumber}</numeroAutorizacion>
  <fechaAutorizacion>{authorizedAt:yyyy-MM-ddTHH:mm:ssZ}</fechaAutorizacion>
  <ambiente>{ambiente}</ambiente>
  <comprobante><![CDATA[{comprobante}]]></comprobante>
</autorizacion>";
    }

    private string BuildAdditionalInfoXml(Invoice invoice)
    {
        var fields = new List<string>();

        if (!string.IsNullOrWhiteSpace(invoice.CustomerEmail))
        {
            fields.Add($@"    <campoAdicional nombre=""Email"">{EscapeXml(invoice.CustomerEmail)}</campoAdicional>");
        }

        if (!string.IsNullOrWhiteSpace(invoice.CustomerPhone))
        {
            fields.Add($@"    <campoAdicional nombre=""Telefono"">{EscapeXml(invoice.CustomerPhone)}</campoAdicional>");
        }

        if (!string.IsNullOrWhiteSpace(invoice.CustomerAddress))
        {
            fields.Add($@"    <campoAdicional nombre=""Direccion"">{EscapeXml(invoice.CustomerAddress)}</campoAdicional>");
        }

        if (!string.IsNullOrWhiteSpace(invoice.Observations))
        {
            fields.Add($@"    <campoAdicional nombre=""Observacion"">{EscapeXml(invoice.Observations)}</campoAdicional>");
        }

        if (fields.Count == 0)
        {
            return string.Empty;
        }

        return $"  <infoAdicional>{Environment.NewLine}{string.Join(Environment.NewLine, fields)}{Environment.NewLine}  </infoAdicional>{Environment.NewLine}";
    }

    private static string MapPaymentMethod(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.Cash => "01",
            PaymentMethod.Card => "19",
            PaymentMethod.Transfer => "20",
            _ => "20"
        };
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static string NormalizeObligadoContabilidad(string? value)
    {
        return string.Equals(value, "NO", StringComparison.OrdinalIgnoreCase) ? "NO" : "SI";
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);
    }

    private static string EscapeCData(string value)
    {
        return value.Replace("]]>", "]]]]><![CDATA[>", StringComparison.Ordinal);
    }

    private static string BuildBaseFileName(string invoiceNumber, string accessKey)
    {
        var safeInvoiceNumber = string.Concat(invoiceNumber.Select(ch =>
            Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        var timestamp = DateTimeHelper.Now().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        return $"{safeInvoiceNumber}_{accessKey}_{timestamp}";
    }

    private static string WriteDocument(string directory, string fileName, string content)
    {
        var fullPath = Path.Combine(directory, fileName);
        File.WriteAllText(fullPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return fullPath;
    }

    private static string ResolveDirectory(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredPath));
    }

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
