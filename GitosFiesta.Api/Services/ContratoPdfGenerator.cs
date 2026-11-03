using GitosFiesta.Api.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GitosFiesta.Api.Services;

public sealed class ContratoPdfGenerator
{
    public byte[] Generate(ContratoPdfRequest data)
    {
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(32);
                page.DefaultTextStyle(style => style.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().Text("GITOS FIESTA").FontSize(24).Bold().FontColor(Colors.DeepPurple.Medium);
                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(16).Column(column =>
                {
                    column.Spacing(8);
                    column.Item().AlignRight().Text($"Folio: {data.Folio ?? data.EventoId.ToString()}").Bold();
                    AddField(column, "Fecha de contrato", data.FechaContrato.ToString("dd/MM/yyyy HH:mm"));
                    AddField(column, "Cliente", data.NombreCliente);
                    AddField(column, "Teléfono", data.TelefonoCliente);
                    AddField(column, "Evento", data.TipoEvento);
                    AddField(column, "Fecha del evento", data.FechaEvento.ToString("dd/MM/yyyy"));
                    AddField(column, "Hora", data.HoraEvento.ToString(@"hh\:mm"));
                    AddField(column, "Personas", data.NumeroPersonas.ToString());
                    AddField(column, "Menú", data.Menu);
                    AddField(column, "Precio", $"${data.Precio:F2}");
                    AddField(column, "Anticipo", $"${data.Anticipo:F2}");
                    AddField(column, "Saldo", $"${data.Saldo:F2}");
                    AddField(column, "Promoción", data.Promocion);
                    AddField(column, "Observaciones", data.Observaciones);

                    if (data.Terminos.Count > 0)
                    {
                        column.Item().PaddingTop(12).Text("TÉRMINOS Y CONDICIONES").Bold();
                        foreach (var termino in data.Terminos)
                            column.Item().Text(termino);
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Documento generado por Gitos Fiesta");
                });
            });
        }).GeneratePdf();
    }

    private static void AddField(ColumnDescriptor column, string label, string? value)
    {
        column.Item().Row(row =>
        {
            row.ConstantItem(125).Text($"{label}:").Bold();
            row.RelativeItem().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Text(value ?? "");
        });
    }
}
