using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MEDICSYS.Api.Models;

namespace MEDICSYS.Api.Services;

public interface IRideService
{
    byte[] GenerateRide(Invoice invoice);
}

public class RideService : IRideService
{
    private readonly SriOptions _options;

    private static readonly string Blue = "#1B3A6B";
    private static readonly string LightBlue = "#E8EDF5";
    private static readonly string Gray = "#F5F5F5";
    private static readonly string DarkGray = "#555555";
    private static readonly string BorderGray = "#CCCCCC";
    private static readonly string YellowBadge = "#FFF3CD";
    private static readonly string GreenBadge = "#D4EDDA";

    public RideService(IOptions<SriOptions> options)
    {
        _options = options.Value;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateRide(Invoice invoice)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Black));

                page.Content().Column(col =>
                {
                    // ─── HEADER ───────────────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        // Left: company info
                        row.RelativeItem(2).Border(1).BorderColor(BorderGray).Padding(8).Column(c =>
                        {
                            c.Item().Text(_options.RazonSocial)
                                .Bold().FontSize(11).FontColor(Blue);

                            if (!string.IsNullOrWhiteSpace(_options.NombreComercial) &&
                                _options.NombreComercial != _options.RazonSocial)
                            {
                                c.Item().PaddingTop(2).Text(_options.NombreComercial)
                                    .FontSize(9).FontColor(DarkGray);
                            }

                            c.Item().PaddingTop(4).Text(text =>
                            {
                                text.Span("Dir. Matriz: ").Bold();
                                text.Span(_options.DireccionMatriz);
                            });

                            if (!string.IsNullOrWhiteSpace(_options.DireccionEstablecimiento))
                            {
                                c.Item().PaddingTop(2).Text(text =>
                                {
                                    text.Span("Dir. Establecimiento: ").Bold();
                                    text.Span(_options.DireccionEstablecimiento);
                                });
                            }

                            if (!string.IsNullOrWhiteSpace(_options.ContribuyenteEspecial))
                            {
                                c.Item().PaddingTop(2).Text(text =>
                                {
                                    text.Span("Contribuyente Especial Nro: ").Bold();
                                    text.Span(_options.ContribuyenteEspecial);
                                });
                            }

                            c.Item().PaddingTop(2).Text(text =>
                            {
                                text.Span("Obligado a llevar Contabilidad: ").Bold();
                                text.Span(_options.ObligadoContabilidad);
                            });
                        });

                        // Right: invoice meta
                        row.RelativeItem(1).Column(c =>
                        {
                            // RUC box
                            c.Item().Border(1).BorderColor(BorderGray).Background(LightBlue)
                                .Padding(6).Column(inner =>
                                {
                                    inner.Item().AlignCenter().Text("R.U.C.")
                                        .Bold().FontSize(8).FontColor(DarkGray);
                                    inner.Item().AlignCenter().Text(_options.Ruc)
                                        .Bold().FontSize(11).FontColor(Blue);
                                });

                            // Document type
                            c.Item().Border(1).BorderColor(BorderGray).Padding(6).Column(inner =>
                            {
                                inner.Item().AlignCenter().Text("FACTURA")
                                    .Bold().FontSize(13).FontColor(Blue);

                                inner.Item().PaddingTop(4).Text(text =>
                                {
                                    text.Span("No. ").Bold();
                                    text.Span(invoice.Number).Bold().FontSize(10);
                                });

                                inner.Item().PaddingTop(2).Text(text =>
                                {
                                    text.Span("NÚMERO DE AUTORIZACIÓN").Bold().FontSize(7).FontColor(DarkGray);
                                });

                                inner.Item().Text(invoice.SriAuthorizationNumber ?? "PENDIENTE")
                                    .FontSize(7).FontColor(Colors.Black);

                                if (invoice.SriAuthorizedAt.HasValue)
                                {
                                    inner.Item().PaddingTop(2).Text(text =>
                                    {
                                        text.Span("FECHA Y HORA DE AUTORIZACIÓN: ").Bold().FontSize(7).FontColor(DarkGray);
                                    });
                                    inner.Item().Text(invoice.SriAuthorizedAt.Value.ToString("dd/MM/yyyy HH:mm:ss"))
                                        .FontSize(7);
                                }

                                // Environment badge
                                var (badgeText, badgeColor) = invoice.SriEnvironment == "Produccion"
                                    ? ("AMBIENTE: PRODUCCIÓN", GreenBadge)
                                    : ("AMBIENTE: PRUEBAS", YellowBadge);

                                inner.Item().PaddingTop(4).Background(badgeColor).Padding(3)
                                    .AlignCenter().Text(badgeText).Bold().FontSize(8);
                            });
                        });
                    });

                    col.Item().Height(6);

                    // ─── BUYER INFO ───────────────────────────────────────────
                    col.Item().Border(1).BorderColor(BorderGray).Column(section =>
                    {
                        section.Item().Background(Blue).Padding(4)
                            .Text("INFORMACIÓN DEL COMPRADOR").Bold().FontSize(8).FontColor(Colors.White);

                        section.Item().Padding(6).Column(g =>
                        {
                            // Row 1: name | ID | date
                            g.Item().Row(r =>
                            {
                                r.RelativeItem(2).Text(text =>
                                {
                                    text.Span("Razón Social / Nombres y Apellidos: ").Bold().FontColor(DarkGray);
                                    text.Span(invoice.CustomerName);
                                });
                                r.RelativeItem(1).Text(text =>
                                {
                                    text.Span($"{GetIdTypeLabel(invoice.CustomerIdentificationType)}: ").Bold().FontColor(DarkGray);
                                    text.Span(invoice.CustomerIdentification);
                                });
                                r.RelativeItem(1).Text(text =>
                                {
                                    text.Span("Fecha Emisión: ").Bold().FontColor(DarkGray);
                                    text.Span(invoice.IssuedAt.ToString("dd/MM/yyyy"));
                                });
                            });

                            // Row 2: address | phone | email (optional fields)
                            if (!string.IsNullOrWhiteSpace(invoice.CustomerAddress) ||
                                !string.IsNullOrWhiteSpace(invoice.CustomerPhone) ||
                                !string.IsNullOrWhiteSpace(invoice.CustomerEmail))
                            {
                                g.Item().PaddingTop(4).Row(r =>
                                {
                                    r.RelativeItem(2).Text(text =>
                                    {
                                        if (!string.IsNullOrWhiteSpace(invoice.CustomerAddress))
                                        {
                                            text.Span("Dirección: ").Bold().FontColor(DarkGray);
                                            text.Span(invoice.CustomerAddress);
                                        }
                                    });
                                    r.RelativeItem(1).Text(text =>
                                    {
                                        if (!string.IsNullOrWhiteSpace(invoice.CustomerPhone))
                                        {
                                            text.Span("Teléfono: ").Bold().FontColor(DarkGray);
                                            text.Span(invoice.CustomerPhone);
                                        }
                                    });
                                    r.RelativeItem(1).Text(text =>
                                    {
                                        if (!string.IsNullOrWhiteSpace(invoice.CustomerEmail))
                                        {
                                            text.Span("Email: ").Bold().FontColor(DarkGray);
                                            text.Span(invoice.CustomerEmail);
                                        }
                                    });
                                });
                            }
                        });
                    });

                    col.Item().Height(6);

                    // ─── ITEMS TABLE ──────────────────────────────────────────
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4); // Descripción
                            cols.RelativeColumn(1); // Cant.
                            cols.RelativeColumn(1.5f); // Precio Unit.
                            cols.RelativeColumn(1); // Desc. %
                            cols.RelativeColumn(1.5f); // Descuento $
                            cols.RelativeColumn(1.5f); // Total sin IVA
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Blue).Padding(4)
                                .Text("DESCRIPCIÓN").Bold().FontSize(8).FontColor(Colors.White);
                            header.Cell().Background(Blue).Padding(4).AlignCenter()
                                .Text("CANT.").Bold().FontSize(8).FontColor(Colors.White);
                            header.Cell().Background(Blue).Padding(4).AlignRight()
                                .Text("PRECIO UNIT.").Bold().FontSize(8).FontColor(Colors.White);
                            header.Cell().Background(Blue).Padding(4).AlignRight()
                                .Text("DESC. %").Bold().FontSize(8).FontColor(Colors.White);
                            header.Cell().Background(Blue).Padding(4).AlignRight()
                                .Text("DESCUENTO").Bold().FontSize(8).FontColor(Colors.White);
                            header.Cell().Background(Blue).Padding(4).AlignRight()
                                .Text("TOT. SIN IVA").Bold().FontSize(8).FontColor(Colors.White);
                        });

                        // Rows
                        var rowIndex = 0;
                        foreach (var item in invoice.Items)
                        {
                            Color bg = rowIndex % 2 == 0 ? Colors.White : (Color)Gray;
                            rowIndex++;

                            var discountAmount = item.Quantity * item.UnitPrice * (item.DiscountPercent / 100m);

                            table.Cell().Background(bg).BorderBottom(1).BorderColor(BorderGray).Padding(4)
                                .Text(item.Description);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor(BorderGray).Padding(4).AlignCenter()
                                .Text(item.Quantity.ToString());
                            table.Cell().Background(bg).BorderBottom(1).BorderColor(BorderGray).Padding(4).AlignRight()
                                .Text(FormatDecimal(item.UnitPrice));
                            table.Cell().Background(bg).BorderBottom(1).BorderColor(BorderGray).Padding(4).AlignRight()
                                .Text(item.DiscountPercent > 0 ? $"{item.DiscountPercent:0.##}%" : "-");
                            table.Cell().Background(bg).BorderBottom(1).BorderColor(BorderGray).Padding(4).AlignRight()
                                .Text(item.DiscountPercent > 0 ? FormatDecimal(discountAmount) : "-");
                            table.Cell().Background(bg).BorderBottom(1).BorderColor(BorderGray).Padding(4).AlignRight()
                                .Text(FormatDecimal(item.Subtotal));
                        }
                    });

                    col.Item().Height(6);

                    // ─── TOTALS + PAYMENT ─────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        // Left: observations + payment
                        row.RelativeItem(2).Column(c =>
                        {
                            if (!string.IsNullOrWhiteSpace(invoice.Observations))
                            {
                                c.Item().Border(1).BorderColor(BorderGray).Padding(6).Column(obs =>
                                {
                                    obs.Item().Text("INFORMACIÓN ADICIONAL").Bold().FontSize(8).FontColor(DarkGray);
                                    obs.Item().PaddingTop(2).Text(invoice.Observations).FontSize(8);
                                });
                                c.Item().Height(4);
                            }

                            c.Item().Border(1).BorderColor(BorderGray).Column(pay =>
                            {
                                pay.Item().Background(Blue).Padding(4)
                                    .Text("FORMA DE PAGO").Bold().FontSize(8).FontColor(Colors.White);

                                pay.Item().Padding(6).Table(pt =>
                                {
                                    pt.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn(2);
                                        cols.RelativeColumn(1);
                                        cols.RelativeColumn(1);
                                    });

                                    pt.Header(h =>
                                    {
                                        h.Cell().Background(LightBlue).Padding(3).Text("FORMA DE PAGO").Bold().FontSize(7);
                                        h.Cell().Background(LightBlue).Padding(3).AlignRight().Text("PLAZO").Bold().FontSize(7);
                                        h.Cell().Background(LightBlue).Padding(3).AlignRight().Text("VALOR").Bold().FontSize(7);
                                    });

                                    pt.Cell().Padding(3).Text(GetPaymentMethodLabel(invoice.PaymentMethod)).FontSize(7);
                                    pt.Cell().Padding(3).AlignRight().Text("0 días").FontSize(7);
                                    pt.Cell().Padding(3).AlignRight().Text(FormatDecimal(invoice.TotalToCharge)).FontSize(7);

                                    if (invoice.PaymentMethod == PaymentMethod.Card && invoice.CardFeeAmount > 0)
                                    {
                                        pt.Cell().ColumnSpan(2).Padding(3).Text(text =>
                                        {
                                            text.DefaultTextStyle(s => s.FontSize(7));
                                            text.Span("Comisión tarjeta");
                                            if (invoice.CardFeePercent.HasValue)
                                                text.Span(string.Format(" ({0:0.##}%)", invoice.CardFeePercent.Value));
                                            text.Span(":");
                                        });
                                        pt.Cell().Padding(3).AlignRight().Text(FormatDecimal(invoice.CardFeeAmount ?? 0m)).FontSize(7);
                                    }

                                    if (!string.IsNullOrWhiteSpace(invoice.CardType) || !string.IsNullOrWhiteSpace(invoice.PaymentReference))
                                    {
                                        pt.Cell().ColumnSpan(3).Padding(3).Text(text =>
                                        {
                                            if (!string.IsNullOrWhiteSpace(invoice.CardType))
                                            {
                                                text.Span("Tarjeta: ").Bold().FontSize(7);
                                                text.Span(invoice.CardType).FontSize(7);
                                                text.Span("  ");
                                            }
                                            if (!string.IsNullOrWhiteSpace(invoice.PaymentReference))
                                            {
                                                text.Span("Ref: ").Bold().FontSize(7);
                                                text.Span(invoice.PaymentReference).FontSize(7);
                                            }
                                        });
                                    }
                                });
                            });
                        });

                        row.ConstantItem(10);

                        // Right: totals
                        row.RelativeItem(1).Border(1).BorderColor(BorderGray).Column(c =>
                        {
                            c.Item().Background(Blue).Padding(4)
                                .Text("RESUMEN").Bold().FontSize(8).FontColor(Colors.White);

                            c.Item().Padding(6).Column(totals =>
                            {
                                TotalRow(totals, "Subtotal sin IVA:", FormatDecimal(invoice.Subtotal - invoice.Tax));
                                TotalRow(totals, "Subtotal con IVA 0%:", "$0.00");
                                TotalRow(totals, "Subtotal con IVA 15%:", FormatDecimal(invoice.Subtotal));
                                TotalRow(totals, "Descuento:", FormatDecimal(invoice.DiscountTotal));
                                TotalRow(totals, "IVA 15%:", FormatDecimal(invoice.Tax));

                                if (invoice.CardFeeAmount > 0)
                                {
                                    TotalRow(totals, "Comisión tarjeta:", FormatDecimal(invoice.CardFeeAmount ?? 0m));
                                }

                                // Grand total
                                totals.Item().PaddingTop(4).BorderTop(1).BorderColor(BorderGray)
                                    .Background(LightBlue).Padding(4).Row(r =>
                                    {
                                        r.RelativeItem().Text("VALOR TOTAL:").Bold().FontSize(9);
                                        r.AutoItem().Text(FormatDecimal(invoice.TotalToCharge))
                                            .Bold().FontSize(9).FontColor(Blue);
                                    });
                            });
                        });
                    });

                    col.Item().Height(6);

                    // ─── ACCESS KEY ───────────────────────────────────────────
                    col.Item().Border(1).BorderColor(BorderGray).Column(section =>
                    {
                        section.Item().Background(LightBlue).Padding(4).Row(r =>
                        {
                            r.AutoItem().Text("CLAVE DE ACCESO: ").Bold().FontSize(8).FontColor(Blue);
                            r.RelativeItem().PaddingLeft(4).Text(FormatAccessKey(invoice.SriAccessKey))
                                .FontFamily("Courier New").FontSize(8).FontColor(Colors.Black);
                        });
                    });

                    col.Item().Height(4);

                    // ─── FOOTER ───────────────────────────────────────────────
                    col.Item().AlignCenter().Text(text =>
                    {
                        text.Span("Documento generado electrónicamente — ").FontSize(7).FontColor(DarkGray);
                        text.Span("Autorizado por el S.R.I.").Bold().FontSize(7).FontColor(Blue);
                    });

                    if (invoice.Status != InvoiceStatus.Authorized)
                    {
                        col.Item().Height(4);
                        col.Item().Background(YellowBadge).Padding(6).AlignCenter()
                            .Text("DOCUMENTO EN PROCESO DE AUTORIZACIÓN — NO VÁLIDO COMO COMPROBANTE")
                            .Bold().FontSize(9).FontColor("#856404");
                    }
                });
            });
        });

        return document.GeneratePdf();
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    private static void TotalRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().PaddingBottom(2).Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(8);
            r.AutoItem().MinWidth(70).AlignRight().Text(value).FontSize(8);
        });
    }

    private static string FormatDecimal(decimal value) =>
        $"${value:N2}";

    private static string FormatAccessKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "N/A";
        // Insert a space every 10 digits for readability
        return string.Join(" ", Enumerable.Range(0, (int)Math.Ceiling(key.Length / 10.0))
            .Select(i => key.Substring(i * 10, Math.Min(10, key.Length - i * 10))));
    }

    private static string GetIdTypeLabel(string idType) => idType switch
    {
        "RUC" => "RUC",
        "CED" or "CI" => "Cédula",
        "PAS" => "Pasaporte",
        "CON" => "Cons. Final",
        _ => idType
    };

    private static string GetPaymentMethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Efectivo",
        PaymentMethod.Card => "Tarjeta de crédito/débito",
        PaymentMethod.Transfer => "Transferencia bancaria",
        PaymentMethod.Other => "Otro",
        _ => method.ToString()
    };
}
