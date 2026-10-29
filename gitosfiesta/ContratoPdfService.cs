using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using gitosfiesta.Models;
using QColors = QuestPDF.Helpers.Colors;

public class ContratoPdfService
{
    private readonly EventoModel _evento;
    private readonly List<string> _terminos;

    public ContratoPdfService(EventoModel evento, List<string> terminos)
    {
        _evento = evento;
        _terminos = terminos;
    }

    public byte[] GenerarPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // Cargar la imagen del logo desde los recursos del dispositivo/aplicación
        byte[]? logoBytes = null;
        try
        {
            string logoPath = Path.Combine(FileSystem.Current.AppDataDirectory, "logo_gitos.png");
            // O alternativamente puedes abrirlo desde los recursos si prefieres cargarlo por stream:
            using var stream = FileSystem.OpenAppPackageFileAsync("logo_gitos.png").Result;
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            logoBytes = memoryStream.ToArray();
        }
        catch
        {
            // Si por alguna razón no encuentra el archivo físico en tiempo de ejecución, manejamos un respaldo nulo
            logoBytes = null;
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(25);
                page.PageColor(QColors.White);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(QColors.Grey.Darken3));

                // MARCA DE AGUA DE FONDO
                page.Background().Layers(layers =>
                {
                    layers.PrimaryLayer().AlignCenter().AlignMiddle().PaddingBottom(20).Text("GITOS FIESTA")
                        .FontSize(50).Bold().FontColor("#F2D6E8").LetterSpacing(0.15f);
                });

                // ENCABEZADO CON EL LOGOTIPO REAL
                page.Header().Row(row =>
                {
                    row.RelativeItem().AlignCenter().Column(col =>
                    {
                        if (logoBytes != null)
                        {
                            // Renderiza la imagen del logotipo con el tamaño proporcional correcto
                            col.Item().Width(180f).Image(logoBytes);
                        }
                        else
                        {
                            // Respaldo de texto si la imagen llegara a faltar
                            col.Item().AlignCenter().Text("G I T O S   F I E S T A").FontSize(22).Bold().FontColor("#FF1493");
                        }
                    });

                    row.ConstantItem(120).AlignRight().Container().Border(1).BorderColor(QColors.Grey.Medium).CornerRadius(4).Padding(5).Column(col =>
                    {
                        col.Item().Text("Folio:").FontSize(9f).Bold();
                        col.Item().Text(_evento.folio ?? "---").FontSize(11.5f).Bold().FontColor("#FF1493");
                    });
                });

                // CONTENIDO PRINCIPAL
                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Spacing(10);

                    // TARJETA DE DATOS DEL EVENTO
                    col.Item().Border(1).BorderColor(QColors.Grey.Lighten1).CornerRadius(6).Padding(10).Column(card =>
                    {
                        card.Spacing(5);

                        card.Item().Text(t => {
                            t.Span("Fecha de contrato: ").Bold();
                            t.Span(_evento.fecha_contrato.ToString("dd/MM/yyyy HH:mm"));
                        });

                        card.Item().Row(r => {
                            r.RelativeItem(3).Text(t => { t.Span("Nombre: ").Bold(); t.Span(_evento.nombre_cliente); });
                            r.RelativeItem(2).Text(t => { t.Span("Tel/Cel: ").Bold(); t.Span(_evento.telefono_cliente); });
                        });

                        card.Item().Row(r => {
                            r.RelativeItem(2).Text(t => { t.Span("Evento: ").Bold(); t.Span(_evento.tipo_evento); });
                            r.RelativeItem(2).Text(t => { t.Span("Fecha: ").Bold(); t.Span(_evento.fecha_evento.ToString("dd/MM/yyyy")); });
                            r.RelativeItem(2).Text(t => { t.Span("Hora: ").Bold(); t.Span(_evento.hora_evento.ToString(@"hh\:mm")); });
                        });

                        card.Item().Row(r => {
                            r.RelativeItem(2).Text(t => { t.Span("No. personas: ").Bold(); t.Span(_evento.numero_personas.ToString()); });
                            r.RelativeItem(4).Text(t => { t.Span("Menú: ").Bold(); t.Span(_evento.menu); });
                        });

                        card.Item().Row(r => {
                            r.RelativeItem(2).Text(t => { t.Span("Precio: ").Bold(); t.Span($"${_evento.precio:F2}"); });
                            r.RelativeItem(2).Text(t => { t.Span("Anticipo: ").Bold(); t.Span($"${_evento.anticipo:F2}"); });
                            r.RelativeItem(2).Text(t => { t.Span("Saldo: ").Bold(); t.Span($"${_evento.Saldo:F2}").FontColor("#FF1493"); });
                        });

                        card.Item().Text(t => {
                            t.Span("Promoción: ").Bold();
                            t.Span(string.IsNullOrEmpty(_evento.promocion) ? "Ninguna" : _evento.promocion);
                        });

                        card.Item().Text(t => {
                            t.Span("OBSERVACIONES:\n").Bold();
                            t.Span(string.IsNullOrEmpty(_evento.observaciones) ? "Ninguna" : _evento.observaciones);
                        });
                    });

                    // TÉRMINOS Y CONDICIONES (Cargados desde el misc)
                    col.Item().PaddingTop(2).Column(term =>
                    {
                        term.Spacing(2f);
                        term.Item().AlignCenter().Text("Términos y Condiciones").Bold().FontSize(11f).FontColor("#2D3748");

                        foreach (var tTexto in _terminos)
                        {
                            term.Item().Text(tTexto).FontSize(8f).LineHeight(1.15f);
                        }
                    });

                    // SECCIÓN DE FIRMA (Alineada a la derecha)
                    // SECCIÓN DE FIRMA (Alineada a la derecha con tamaño ampliado)
                    col.Item().PaddingTop(10).AlignRight().Column(firmaCol =>
                    {
                        firmaCol.Spacing(4);

                        if (!string.IsNullOrEmpty(_evento.firma))
                        {
                            try
                            {
                                string base64Data = _evento.firma;
                                if (base64Data.Contains(","))
                                {
                                    base64Data = base64Data.Split(',')[1];
                                }

                                byte[] imageBytes = Convert.FromBase64String(base64Data);
                                // Aumentamos el tamaño de la imagen para que luzca más grande
                                firmaCol.Item().Width(240f).Height(85f).Image(imageBytes);
                            }
                            catch
                            {
                                firmaCol.Item().Text("[Firma no válida]").FontSize(8f).Italic().FontColor(QColors.Grey.Medium);
                            }
                        }

                        // Ampliamos también la línea de abajo para mantener la proporción
                        firmaCol.Item().Width(260f).Height(1f).Background(QColors.Grey.Darken2);
                        firmaCol.Item().AlignCenter().Text("FIRMA").Bold().FontSize(9f).FontColor("#2D3748");
                    });
                });

                // PIE DE PÁGINA
                page.Footer().Background("#FF1493").Padding(6).Column(footer =>
                {
                    footer.Item().AlignCenter().Text("16 de Agosto No. 101-D, Col. Reserva de San Roque, Esquina con prolongación Pablo Livas, Juárez N.L.")
                        .FontSize(7.5f).FontColor(QColors.White);
                    footer.Item().AlignCenter().Text("Cel/Whatsapp: 8119887387    |    facebook/GitosFiesta")
                        .FontSize(7.5f).FontColor(QColors.White).Bold();
                });
            });
        });

        return document.GeneratePdf();
    }
}