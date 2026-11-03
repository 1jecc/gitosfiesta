using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using gitosfiesta.Models;
using QColors = QuestPDF.Helpers.Colors;
using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;

public class ContratoPdfService
{
    private readonly EventoModel _evento;
    private readonly List<string> _terminos;

    public ContratoPdfService(EventoModel evento, List<string> terminos)
    {
        _evento = evento;
        _terminos = terminos ?? new List<string>();
    }

    public byte[] GenerarPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        byte[]? logoBytes = ObtenerLogoBytes();
        byte[]? watermarkBytes = ObtenerLogoUltraTransparenteBytes();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(20f);
                page.PageColor(QColors.White);

                page.DefaultTextStyle(x =>
                    x.FontSize(13f)
                     .FontColor(QColors.Black));

                // ==========================================
                // 1. MARCA DE AGUA DE FONDO (DISCRETA Y DETRÁS DE TODO)
                // ==========================================

                if (watermarkBytes != null)
                {
                    page.Background().Layers(layers =>
                    {
                        layers.PrimaryLayer()
                            .AlignCenter()
                            .AlignBottom() // Alineado hacia la parte inferior
                            .PaddingBottom(20f) // Espacio para que no pise el pie de página morado
                            .Width(600f)
                            .Image(watermarkBytes)
                            .FitArea();
                    });
                }


                // ==========================================
                // 2. CONTENIDO PRINCIPAL CON CAPAS (LAYERS)
                // ==========================================
                page.Content().Layers(layers =>
                {
                    // CAPA 1 (FONDO): Logotipo y Cuadro de Folio juntos en un Row centrado
                    layers.PrimaryLayer().Column(col =>
                    {
                        col.Item().AlignRight().Row(r =>
                        {
                            // 1. Logotipo centrado
                            r.AutoItem().Column(c =>
                            {
                                if (logoBytes != null)
                                {
                                    c.Item()
                                        .Width(360f)
                                        .Height(240f)
                                        .Image(logoBytes)
                                        .FitArea();
                                }
                            });

                            // 2. Cuadro de Folio pegado al lado derecho del logotipo con un pequeño margen
                            r.AutoItem().PaddingLeft(15f).AlignTop().Column(folioCol =>
                            {
                                folioCol.Item()
                                    .Border(1.5f)
                                    .BorderColor(QColors.Grey.Lighten1)
                                    .CornerRadius(8f)
                                    .Padding(8f)
                                    .Column(box =>
                                    {
                                        box.Item()
                                            .Text("Folio:")
                                            .FontSize(12f)
                                            .Bold();

                                        box.Item()
                                            .Text(_evento.folio ?? "---")
                                            .FontSize(16f)
                                            .Bold()
                                            .FontColor("#FF1493");
                                    });
                            });
                        });
                    });

                    // CAPA 2 (SUPERIOR): Texto del contrato sobrepuesto
                    layers.Layer().Column(col =>
                    {
                        col.Item().TranslateY(145f).Column(infoCol =>
                        {
                            infoCol.Spacing(6f);

                            // FECHA DEL CONTRATO
                            infoCol.Item().Row(r =>
                            {
                                r.ConstantItem(120f)
                                    .Text("Fecha de contrato:")
                                    .Bold();

                                r.RelativeItem()
                                    .BorderBottom(1f)
                                    .Text(_evento.fecha_contrato.ToString("dd/MM/yyyy HH:mm"));
                            });

                            // NOMBRE Y TELÉFONO
                            infoCol.Item().Row(r =>
                            {
                                r.ConstantItem(60f)
                                    .Text("Nombre:")
                                    .Bold();

                                r.RelativeItem(2f)
                                    .BorderBottom(1f)
                                    .Text(_evento.nombre_cliente);

                                r.ConstantItem(60f)
                                    .PaddingLeft(10f)
                                    .Text("Tel/Cel:")
                                    .Bold();

                                r.RelativeItem(1f)
                                    .BorderBottom(1f)
                                    .Text(_evento.telefono_cliente);
                            });

                            // EVENTO, FECHA Y HORA
                            infoCol.Item().Row(r =>
                            {
                                r.ConstantItem(55f)
                                    .Text("Evento:")
                                    .Bold();

                                r.RelativeItem(1.3f)
                                    .BorderBottom(1f)
                                    .Text(_evento.tipo_evento);

                                r.ConstantItem(45f)
                                    .PaddingLeft(5f)
                                    .Text("Fecha:")
                                    .Bold();

                                r.RelativeItem(1f)
                                    .BorderBottom(1f)
                                    .Text(_evento.fecha_evento.ToString("dd/MM/yyyy"));

                                r.ConstantItem(40f)
                                    .PaddingLeft(5f)
                                    .Text("Hora:")
                                    .Bold();

                                r.RelativeItem(0.7f)
                                    .BorderBottom(1f)
                                    .Text(_evento.hora_evento.ToString(@"hh\:mm"));
                            });

                            // PERSONAS Y MENÚ
                            infoCol.Item().Row(r =>
                            {
                                r.ConstantItem(95f).Text("No. personas:").Bold();
                                r.RelativeItem(1f).Column(c =>
                                {
                                    c.Item().AlignCenter().Text(_evento.numero_personas.ToString());
                                    c.Item().BorderBottom(1f);
                                });

                                r.ConstantItem(50f).PaddingLeft(15f).Text("Menú").Bold();
                                r.RelativeItem(2.5f).Column(c =>
                                {
                                    c.Item().AlignCenter().Text(_evento.menu ?? "");
                                    c.Item().BorderBottom(1f);
                                });
                            });

                            // PRECIOS
                            infoCol.Item().PaddingTop(4f).Row(r =>
                            {
                                r.ConstantItem(55f).Text("Precio:").Bold();
                                r.RelativeItem(1f).Column(c =>
                                {
                                    c.Item().AlignCenter().Text($"${_evento.precio:N2}");
                                    c.Item().BorderBottom(1f);
                                });

                                r.ConstantItem(65f).PaddingLeft(10f).Text("Anticipo:").Bold();
                                r.RelativeItem(1f).Column(c =>
                                {
                                    c.Item().AlignCenter().Text($"${_evento.anticipo:N2}");
                                    c.Item().BorderBottom(1f);
                                });

                                r.ConstantItem(45f).PaddingLeft(10f).Text("Saldo").Bold();
                                r.RelativeItem(1f).Column(c =>
                                {
                                    c.Item().AlignCenter().Text($"${_evento.Saldo:N2}");
                                    c.Item().BorderBottom(1f);
                                });
                            });

                            // PROMOCIÓN
                            infoCol.Item().Row(r =>
                            {
                                r.ConstantItem(75f)
                                    .Text("Promoción:")
                                    .Bold();

                                r.RelativeItem()
                                    .BorderBottom(1f)
                                    .Text(_evento.promocion ?? "");
                            });

                            // OBSERVACIONES
                            infoCol.Item()
                                .PaddingTop(4f)
                                .Column(obs =>
                                {
                                    obs.Spacing(4f);
                                    obs.Item()
                                        .Text("OBSERVACIONES:")
                                        .Bold()
                                        .FontSize(13f);

                                    obs.Item().Layers(layersObs =>
                                    {
                                        layersObs.PrimaryLayer().Column(linesCol =>
                                        {
                                            linesCol.Spacing(18f);
                                            for (int i = 0; i < 4; i++)
                                            {
                                                linesCol.Item().Height(1f).BorderBottom(1f).BorderColor(QColors.Black);
                                            }
                                        });

                                        layersObs.Layer().PaddingTop(1f).Column(textCol =>
                                        {
                                            if (!string.IsNullOrWhiteSpace(_evento.observaciones))
                                            {
                                                textCol.Item()
                                                    .Text(_evento.observaciones)
                                                    .FontSize(12f)
                                                    .LineHeight(1.5f);
                                            }
                                        });
                                    });
                                });

                            // TÉRMINOS Y CONDICIONES
                            infoCol.Item()
                                .PaddingTop(4f)
                                .Column(term =>
                                {
                                    term.Spacing(2f);

                                    foreach (var texto in _terminos)
                                    {
                                        term.Item()
                                            .Text(texto)
                                            .FontSize(12f)
                                            .LineHeight(1f);
                                    }
                                });

                            // FIRMA
                            infoCol.Item()
                                .PaddingTop(10f)
                                .AlignRight()
                                .Width(240f)
                                .Column(firma =>
                                {
                                    if (!string.IsNullOrEmpty(_evento.firma))
                                    {
                                        try
                                        {
                                            string base64 = _evento.firma;

                                            if (base64.Contains(","))
                                                base64 = base64.Split(',')[1];

                                            byte[] firmaBytes =
                                                Convert.FromBase64String(base64);

                                            firma.Item()
                                                .Width(200f)
                                                .Height(60f)
                                                .Image(firmaBytes)
                                                .FitArea();
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine(
                                                $"Error cargando firma: {ex.Message}");
                                        }
                                    }

                                    firma.Item()
                                        .Width(220f)
                                        .BorderBottom(1f);

                                    firma.Item()
                                        .AlignCenter()
                                        .Text("FIRMA")
                                        .Bold()
                                        .FontSize(16f);
                                });
                        });
                    });
                });

                // ==========================================
                // 3. PIE DE PÁGINA (INAMOVIBLE Y LIMPIO)
                // ==========================================
                page.Footer()
                    .Background("#A000A0")
                    .Padding(4f)
                    .Column(footer =>
                    {
                        footer.Item()
                            .AlignCenter()
                            .Text("16 de Agosto No. 101-D, Col. Reserva de San Roque, Esquina con prolongación Pablo Livas, Juárez N.L.")
                            .FontColor(QColors.Black)
                            .FontSize(11f);

                        footer.Item()
                            .AlignCenter()
                            .Text("Cel/Whatsapp 8119887387    facebook/GitosFiesta")
                            .FontColor(QColors.Black)
                            .FontSize(11f)
                            .Bold();
                    });
            });
        });

        return document.GeneratePdf();
    }

    // ==========================================
    // 4. MÉTODOS DE CARGA DE IMÁGENES
    // ==========================================
    private byte[]? ObtenerLogoBytes()
    {
        try
        {
            using var stream = FileSystem
                .OpenAppPackageFileAsync("imagen3.png")
                .GetAwaiter()
                .GetResult();

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR AL CARGAR LOGO: {ex}");
            return null;
        }
    }

    private byte[]? ObtenerLogoUltraTransparenteBytes()
    {
        try
        {
            using var stream = FileSystem
                .OpenAppPackageFileAsync("imagen3.png")
                .GetAwaiter()
                .GetResult();

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            byte[] originalBytes = memoryStream.ToArray();

            using var inputStream = new SKMemoryStream(originalBytes);
            using var originalBitmap = SKBitmap.Decode(inputStream);

            using var transparentBitmap = new SKBitmap(originalBitmap.Width, originalBitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var canvas = new SKCanvas(transparentBitmap);

            using var paint = new SKPaint();
            paint.Color = new SKColor(255, 255, 255, 45); // Súper transparente
            paint.BlendMode = SKBlendMode.SrcOver;

            canvas.DrawBitmap(originalBitmap, 0, 0, paint);

            using var image = SKImage.FromBitmap(transparentBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al aplicar transparencia al fondo: {ex.Message}");
            return null;
        }
    }
}