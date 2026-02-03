using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using MEDICSYS.Api.Models;

namespace MEDICSYS.Api.Services;

public record SriSendResult(string Status, string AccessKey, string? AuthorizationNumber, DateTime? AuthorizedAt, string? Messages);

public interface ISriService
{
    Task<SriSendResult> SendInvoiceAsync(Invoice invoice);
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
}

public class SriService : ISriService
{
    private readonly SriOptions _options;
    private readonly ILogger<SriService> _logger;

    public SriService(IOptions<SriOptions> options, ILogger<SriService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<SriSendResult> SendInvoiceAsync(Invoice invoice)
    {
        var accessKey = GenerateAccessKey(invoice.IssuedAt, invoice.Sequential);

        if (_options.Mock)
        {
            var authorized = invoice.Total > 0;
            var status = authorized ? "AUTORIZADO" : "RECHAZADO";
            var authorization = authorized ? GenerateAuthorizationNumber() : null;
            var message = authorized ? null : "Monto inválido para autorización.";

            _logger.LogInformation("SRI MOCK: {Status} para factura {Number}", status, invoice.Number);
            return Task.FromResult(new SriSendResult(status, accessKey, authorization, authorized ? DateTime.UtcNow : null, message));
        }

        _logger.LogWarning("SRI real no configurado. Se deja la factura en estado PENDIENTE.");
        return Task.FromResult(new SriSendResult("PENDIENTE", accessKey, null, null, "Envío real al SRI no configurado."));
    }

    private string GenerateAccessKey(DateTime date, int sequential)
    {
        var dia = date.Day.ToString().PadLeft(2, '0');
        var mes = date.Month.ToString().PadLeft(2, '0');
        var anio = date.Year.ToString();
        var tipoComprobante = "01";
        var ambiente = _options.Ambiente.Equals("Produccion", StringComparison.OrdinalIgnoreCase) ? "2" : "1";
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
        var resultado = residuo == 0 ? 0 : 11 - residuo;
        return resultado.ToString();
    }

    private static string GenerateAuthorizationNumber()
    {
        return $"{DateTime.UtcNow:ddMMyyyyHHmmss}{RandomNumberGenerator.GetInt32(100000, 999999)}";
    }
}
