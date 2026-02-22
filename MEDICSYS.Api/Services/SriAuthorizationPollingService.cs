using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models;

namespace MEDICSYS.Api.Services;

/// <summary>
/// Servicio en segundo plano que consulta periódicamente el SRI para actualizar
/// el estado de las facturas pendientes de autorización.
/// Cuando el SRI devuelve [70] "CLAVE EN PROCESAMIENTO", el comprobante queda en
/// estado Pending en la base de datos y este servicio lo lleva a Authorized / Rejected
/// automáticamente en cuanto el SRI responda.
/// </summary>
public class SriAuthorizationPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SriAuthorizationPollingService> _logger;

    /// Intervalo entre cada ciclo de consulta al SRI.
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    /// No consultar facturas con más de 48 h de antigüedad (SRI no las autorizará).
    private static readonly TimeSpan MaxPollAge = TimeSpan.FromHours(48);

    public SriAuthorizationPollingService(
        IServiceScopeFactory scopeFactory,
        ILogger<SriAuthorizationPollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Servicio de polling SRI iniciado. Consultará el estado de facturas pendientes cada {Seconds} segundos.",
            PollInterval.TotalSeconds);

        // Pequeña espera inicial para que la API termine de inicializarse.
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollPendingInvoicesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el ciclo de polling de autorización SRI");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }

        _logger.LogInformation("Servicio de polling SRI detenido.");
    }

    private async Task PollPendingInvoicesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OdontologoDbContext>();
        var sri = scope.ServiceProvider.GetRequiredService<ISriService>();

        // Solo facturas pendientes con clave de acceso generada, dentro del período válido del SRI.
        var cutoff = DateTimeHelper.Now().AddHours(-MaxPollAge.TotalHours);

        var pendingInvoices = await db.Invoices
            .Where(i =>
                i.Status == InvoiceStatus.Pending &&
                i.SriAccessKey != null &&
                i.SriAccessKey != "" &&
                i.IssuedAt >= cutoff)
            .ToListAsync(ct);

        if (pendingInvoices.Count == 0) return;

        _logger.LogInformation(
            "Polling SRI: {Count} factura(s) pendiente(s) de autorización encontrada(s).",
            pendingInvoices.Count);

        foreach (var invoice in pendingInvoices)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var result = await sri.QueryAuthorizationAsync(invoice.SriAccessKey!, invoice.SriEnvironment);

                _logger.LogDebug(
                    "Polling factura {Number} (clave …{Suffix}): SRI respondió '{Status}'",
                    invoice.Number,
                    invoice.SriAccessKey!.Length > 8 ? invoice.SriAccessKey[^8..] : invoice.SriAccessKey,
                    result.Status);

                switch (result.Status)
                {
                    case "AUTORIZADO":
                        invoice.Status = InvoiceStatus.Authorized;
                        invoice.SriAuthorizationNumber = result.AuthorizationNumber;
                        invoice.SriAuthorizedAt = result.AuthorizedAt;
                        invoice.SriMessages = null; // Limpiar mensajes de error anteriores
                        invoice.UpdatedAt = DateTimeHelper.Now();
                        await db.SaveChangesAsync(ct);
                        _logger.LogInformation(
                            "✓ Factura {Number} AUTORIZADA por el SRI. N° autorización: {Auth}",
                            invoice.Number, result.AuthorizationNumber);
                        break;

                    case "NO AUTORIZADO":
                        invoice.Status = InvoiceStatus.Rejected;
                        invoice.SriMessages = result.Messages;
                        invoice.UpdatedAt = DateTimeHelper.Now();
                        await db.SaveChangesAsync(ct);
                        _logger.LogWarning(
                            "✗ Factura {Number} NO AUTORIZADA por el SRI: {Messages}",
                            invoice.Number, result.Messages);
                        break;

                    default:
                        // Sigue EN PROCESO — se reintentará en el próximo ciclo.
                        _logger.LogDebug(
                            "Factura {Number} sigue en procesamiento en el SRI. Se reintentará en {Seconds}s.",
                            invoice.Number, PollInterval.TotalSeconds);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error al consultar autorización SRI para factura {Number}", invoice.Number);
            }
        }
    }
}
