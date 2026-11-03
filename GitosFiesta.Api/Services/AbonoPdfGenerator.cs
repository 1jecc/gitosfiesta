using GitosFiesta.Api.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace GitosFiesta.Api.Services;

public sealed class AbonoPdfGenerator
{
    public byte[] Generate(AbonoPdfRequest data)
    {
        var saldoActual = Math.Max(0, data.SaldoAnterior - data.Monto);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(style => style.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().Text("GITOS FIESTA")
                        .FontSize(24).Bold().FontColor(Colors.Teal.Medium);
                    column.Item().Text("COMPROBANTE DE ABONO")
                        .FontSize(15).Bold();
                });

                page.Content().PaddingTop(20).Column(column =>
                {
                    column.Spacing(10);
                    AddField(column, "Folio del evento", data.FolioEvento);
                    AddField(column, "Cliente", data.NombreCliente);
                    AddField(column, "Teléfono", data.TelefonoCliente);
                    AddField(column, "Evento", data.TipoEvento);
                    AddField(column, "Fecha del evento", data.FechaEvento.ToString("dd/MM/yyyy"));
                    AddField(column, "Fecha de pago", data.FechaPago.ToString("dd/MM/yyyy HH:mm"));
                    AddField(column, "Monto abonado", $"${data.Monto:F2}");
                    AddField(column, "Método de pago", data.MetodoPago);
                    AddField(column, "Saldo anterior", $"${data.SaldoAnterior:F2}");
                    AddField(column, "Saldo actual", $"${saldoActual:F2}");
                });

                page.Footer().AlignCenter().Text("Documento generado por Gitos Fiesta");
            });
        }).GeneratePdf();
    }

    private static void AddField(ColumnDescriptor column, string label, string? value)
    {
        column.Item().Row(row =>
        {
            row.ConstantItem(140).Text($"{label}:").Bold();
            row.RelativeItem().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Text(value ?? "");
        });
    }
}
