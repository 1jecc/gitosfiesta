using GitosFiesta.Api.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GitosFiesta.Api.Services;

public sealed class ContratoPdfGenerator
{
    public byte[] Generate(ContratoPdfRequest data)
    {
        byte[]? logo = Decode(data.LogoBase64);
        byte[]? watermark = Decode(data.WatermarkBase64);
        byte[]? firma = Decode(data.FirmaBase64);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginTop(20);
                page.MarginBottom(20);
                page.MarginLeft(20);
                page.MarginRight(20);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style.FontSize(13).FontColor(Colors.Black));

                if (watermark is not null)
                {
                    page.Background().Layers(layers =>
                    {
                        layers.PrimaryLayer()
                            .AlignCenter()
                            .AlignBottom()
                            .PaddingBottom(20)
                            .Width(600)
                            .Image(watermark)
                            .FitArea();
                    });
                }

                page.Content().Layers(layers =>
                {
                    layers.PrimaryLayer().Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            row.AutoItem().TranslateY(-55).AlignLeft().Column(logoColumn =>
                            {
                                if (logo is not null)
                                    logoColumn.Item().Width(360).Height(250).Image(logo).FitArea();
                            });

                            row.RelativeItem();
                            row.AutoItem().TranslateY(-5).AlignRight().Column(folio =>
                            {
                                folio.Item().Border(1.5f).BorderColor(Colors.Grey.Lighten1)
                                    .CornerRadius(8).Padding(8).Column(box =>
                                    {
                                        box.Item().Text("Folio:").FontSize(12).Bold();
                                        box.Item().Text(data.Folio ?? $"#{data.EventoId}")
                                            .FontSize(16).Bold().FontColor("#FF1493");
                                    });
                            });
                        });
                    });

                    layers.Layer().Column(column =>
                    {
                        column.Item().TranslateY(135).Column(info =>
                        {
                            info.Spacing(6);
                            Field(info, "Fecha de contrato", data.FechaContrato.ToString("dd/MM/yyyy HH:mm"));
                            Pair(info, "Nombre", data.NombreCliente, "Tel/Cel", data.TelefonoCliente);
                            Pair(info, "Evento", data.TipoEvento, "Fecha", data.FechaEvento.ToString("dd/MM/yyyy"), "Hora", data.HoraEvento.ToString(@"hh\:mm"));
                            Pair(info, "No. personas", data.NumeroPersonas.ToString(), "Menú", data.Menu);
                            Pair(info, "Precio", $"${data.Precio:N2}", "Anticipo", $"${data.Anticipo:N2}", "Saldo", $"${data.Saldo:N2}");
                            Field(info, "Promoción", data.Promocion);

                            info.Item().PaddingTop(4).Column(observaciones =>
                            {
                                observaciones.Spacing(4);
                                observaciones.Item().Text("OBSERVACIONES:").Bold().FontSize(13);
                                observaciones.Item().Layers(lines =>
                                {
                                    lines.PrimaryLayer().Column(lineColumn =>
                                    {
                                        lineColumn.Spacing(18);
                                        for (var i = 0; i < 4; i++)
                                            lineColumn.Item().Height(1).BorderBottom(1).BorderColor(Colors.Black);
                                    });
                                    if (!string.IsNullOrWhiteSpace(data.Observaciones))
                                        lines.Layer().PaddingTop(1).Text(data.Observaciones).FontSize(12).LineHeight(1.5f);
                                });
                            });

                            if (data.Terminos.Count > 0)
                            {
                                info.Item().PaddingTop(4).Column(terms =>
                                {
                                    terms.Spacing(2);
                                    terms.Item().Text("TÉRMINOS Y CONDICIONES").Bold();
                                    foreach (var term in data.Terminos)
                                        terms.Item().Text(term).FontSize(12).LineHeight(1);
                                });
                            }

                            info.Item().PaddingTop(10).AlignRight().Width(240).Column(signature =>
                            {
                                if (firma is not null)
                                    signature.Item().Width(200).Height(60).Image(firma).FitArea();
                                signature.Item().Width(220).BorderBottom(1);
                                signature.Item().AlignCenter().Text("FIRMA").Bold().FontSize(16);
                            });
                        });
                    });
                });

                page.Footer().Background("#A000A0").Padding(4).Column(footer =>
                {
                    footer.Item().AlignCenter().Text("16 de Agosto No. 101-D, Col. Reserva de San Roque, Esquina con prolongación Pablo Livas, Juárez N.L.").FontColor(Colors.Black).FontSize(11);
                    footer.Item().AlignCenter().Text("Cel/Whatsapp 8119887387    facebook/GitosFiesta").FontColor(Colors.Black).FontSize(11).Bold();
                });
            });
        }).GeneratePdf();
    }

    private static void Field(ColumnDescriptor column, string label, string? value)
    {
        column.Item().Row(row =>
        {
            row.ConstantItem(120).Text($"{label}:").Bold();
            row.RelativeItem().BorderBottom(1).Text(value ?? string.Empty);
        });
    }

    private static void Pair(ColumnDescriptor column, string label1, string? value1, string label2, string? value2, string? label3 = null, string? value3 = null)
    {
        column.Item().Row(row =>
        {
            row.ConstantItem(55).Text($"{label1}:").Bold();
            row.RelativeItem(1).BorderBottom(1).Text(value1 ?? string.Empty);
            row.ConstantItem(55).PaddingLeft(6).Text($"{label2}:").Bold();
            row.RelativeItem(1).BorderBottom(1).Text(value2 ?? string.Empty);
            if (label3 is not null)
            {
                row.ConstantItem(40).PaddingLeft(6).Text($"{label3}:").Bold();
                row.RelativeItem(1).BorderBottom(1).Text(value3 ?? string.Empty);
            }
        });
    }

    private static byte[]? Decode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        try
        {
            var base64 = value.Contains(',') ? value[(value.IndexOf(',') + 1)..] : value;
            return Convert.FromBase64String(base64);
        }
        catch
        {
            return null;
        }
    }
}
