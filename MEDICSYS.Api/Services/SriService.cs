using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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

public interface ISriService
{
    Task<SriSendResult> SendInvoiceAsync(Invoice invoice, string? environment = null);
}

public class SriOptions
{
    public bool Mock { get; set; } = true;
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
}

public class SriService : ISriService
{
    private const decimal VatRate = 15m;

    private readonly SriOptions _options;
    private readonly ILogger<SriService> _logger;
    private readonly string _docGeneradosDir;
    private readonly string _docAutorizadosDir;
    private readonly string _docFirmadosDir;
    private readonly string _docRespuestasDir;

    public SriService(IOptions<SriOptions> options, ILogger<SriService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var root = ResolveDirectory(_options.RutaDocumentos);
        _docGeneradosDir = EnsureDirectory(Path.Combine(root, _options.CarpetaDocGenerados));
        _docAutorizadosDir = EnsureDirectory(Path.Combine(root, _options.CarpetaDocAutorizados));
        _docFirmadosDir = EnsureDirectory(Path.Combine(root, _options.CarpetaDocFirmados));
        _docRespuestasDir = EnsureDirectory(Path.Combine(root, _options.CarpetaDocRespuestas));
    }

    public Task<SriSendResult> SendInvoiceAsync(Invoice invoice, string? environment = null)
    {
        var environmentValue = NormalizeEnvironment(environment ?? invoice.SriEnvironment);
        var accessKey = GenerateAccessKey(invoice.IssuedAt, invoice.Sequential, environmentValue);
        var xml = BuildInvoiceXml(invoice, accessKey, environmentValue);
        var baseName = BuildBaseFileName(invoice.Number, accessKey);

        var generatedXmlPath = WriteDocument(_docGeneradosDir, $"{baseName}.xml", xml);
        var signedXml = BuildSignedXmlMock(xml);
        var signedXmlPath = WriteDocument(_docFirmadosDir, $"{baseName}.xml", signedXml);

        if (_options.Mock)
        {
            var authorized = invoice.Total > 0;
            var status = authorized ? "AUTORIZADO" : "RECHAZADO";
            var authorization = authorized ? GenerateAuthorizationNumber() : null;
            var message = authorized ? null : "Monto inválido para autorización.";
            var authorizedAt = authorized ? DateTime.UtcNow : (DateTime?)null;

            var responseXml = BuildReceptionResponseXml(status, accessKey, message);
            var responseXmlPath = WriteDocument(_docRespuestasDir, $"{baseName}.xml", responseXml);

            string? authorizedXmlPath = null;
            if (authorized && authorization != null && authorizedAt.HasValue)
            {
                var authorizationXml = BuildAuthorizationXml(authorization, authorizedAt.Value, environmentValue, signedXml);
                authorizedXmlPath = WriteDocument(_docAutorizadosDir, $"{baseName}.xml", authorizationXml);
            }

            _logger.LogInformation("SRI MOCK {Environment}: {Status} para factura {Number}", environmentValue, status, invoice.Number);
            return Task.FromResult(
                new SriSendResult(
                    status,
                    accessKey,
                    authorization,
                    authorizedAt,
                    message,
                    generatedXmlPath,
                    signedXmlPath,
                    responseXmlPath,
                    authorizedXmlPath));
        }

        var pendingResponseXml = BuildReceptionResponseXml("PENDIENTE", accessKey, "Envío real al SRI no configurado.");
        var pendingResponsePath = WriteDocument(_docRespuestasDir, $"{baseName}.xml", pendingResponseXml);

        _logger.LogWarning("SRI {Environment} real no configurado. Se deja la factura en estado PENDIENTE.", environmentValue);
        return Task.FromResult(
            new SriSendResult(
                "PENDIENTE",
                accessKey,
                null,
                null,
                "Envío real al SRI no configurado.",
                generatedXmlPath,
                signedXmlPath,
                pendingResponsePath,
                null));
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

    private static string GenerateAuthorizationNumber()
    {
        return $"{DateTime.UtcNow:ddMMyyyyHHmmss}{RandomNumberGenerator.GetInt32(100000, 999999)}";
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
    <direccionComprador>{EscapeXml(invoice.CustomerAddress ?? string.Empty)}</direccionComprador>
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

    private static string BuildSignedXmlMock(string xml)
    {
        return xml;
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
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
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
