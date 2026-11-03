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
                page.MarginTop(20f);
                page.MarginBottom(20f);
                page.MarginLeft(20f);
                page.MarginRight(20f);
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
                    // CAPA 1 (FONDO): Logotipo a la izquierda y Folio a la derecha de forma independiente
                    layers.PrimaryLayer().Column(col =>
                    {
                        // Contenedor principal en fila para separar ambos extremos (Izquierda y Derecha)
                        col.Item().Row(r =>
                        {
                            // 1. ZONA IZQUIERDA: Logotipo con su propio ajuste vertical
                            r.AutoItem().TranslateY(-55f).AlignLeft().Column(c =>
                            {
                                if (logoBytes != null)
                                {
                                    c.Item()
                                      .Width(360f)
                                      .Height(250f)
                                      .Image(logoBytes)
                                      .FitArea();
                                }
                            });

                            // Espacio flexible que empuja el folio hacia el extremo derecho
                            r.RelativeItem();

                            // 2. ZONA DERECHA: Cuadro de Folio en la esquina superior derecha
                            r.AutoItem().TranslateY(-5f).AlignRight().Column(folioCol =>
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
                        col.Item().TranslateY(135f).Column(infoCol =>
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
#if ANDROID
            // Lectura segura para Android usando los Assets nativos
            using var stream = Microsoft.Maui.ApplicationModel.Platform.AppContext.Assets.Open("imagen3.png");
#else
        // Lectura para Windows y otras plataformas
        using var stream = FileSystem.OpenAppPackageFileAsync("imagen3.png").GetAwaiter().GetResult();
#endif
            if (stream == null) return null;

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            // Si hay un error, lo registra en la consola de depuración en lugar de cerrar la app
            System.Diagnostics.Debug.WriteLine($"ADVERTENCIA AL CARGAR LOGO EN ABONO: {ex.Message}");
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