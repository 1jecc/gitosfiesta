using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using gitosfiesta.Models;
using QColors = QuestPDF.Helpers.Colors;
using System.IO;

namespace gitosfiesta
{
    public class AbonoPdfService
    {
        private readonly EventoModel _evento;
        private readonly AbonoModel _abono;
        private readonly decimal _saldoAnterior;

        // Definición de colores corporativos basados en la imagen de referencia
        private static readonly string ColorPrimario = "#00D2C4"; // Turquesa corporativo
        private static readonly string ColorTextoOscuro = "#1E293B"; // Slate oscuro
        private static readonly string ColorFondoCaja = "#F8FAFC"; // Gris muy tenue para contenedores
        private static readonly string ColorBordeCaja = "#E2E8F0"; // Borde suave
        private static readonly string ColorFondoResaltado = "#E6FFFA"; // Turquesa muy claro para filas de abono

        public AbonoPdfService(EventoModel evento, AbonoModel abono, decimal saldoAnterior)
        {
            _evento = evento;
            _abono = abono;
            _saldoAnterior = saldoAnterior;
        }

        public byte[] GenerarPdf()
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(QColors.Grey.Darken3));

                    page.Header().Element(ConstruirEncabezado);
                    page.Content().Element(ConstruirContenido);
                    page.Footer().Element(ConstruirPieDePagina);
                });
            });

            return documento.GeneratePdf();
        }

        private void ConstruirEncabezado(QuestPDF.Infrastructure.IContainer container)
        {
            container.Column(headerCol =>
            {
                headerCol.Item().Row(row =>
                {
                    // Izquierda: Logo (imagen2.png) y subtítulo
                    row.RelativeItem().Column(col =>
                    {
                        // Intentamos cargar imagen2.png desde los recursos o ruta local si está disponible
                        string logoPath = Path.Combine(FileSystem.AppDataDirectory, "..", "..", "Resources", "Images", "imagen2.png");
                        // Como alternativa segura para proyectos MAUI empaquetados o ruta directa:
                        if (File.Exists("imagen2.png"))
                        {
                            col.Item().Width(140).Image("imagen2.png");
                        }
                        else if (File.Exists(@"Resources\Images\imagen2.png"))
                        {
                            col.Item().Width(140).Image(@"Resources\Images\imagen2.png");
                        }
                        else
                        {
                            // Texto de respaldo si la ruta varía en compilación
                            col.Item().Text("GITOS FIESTA").Bold().FontSize(20).FontColor(ColorPrimario);
                        }

                        col.Item().PaddingTop(2).Text("Salón de Fiestas Infantiles").FontSize(9).FontColor(QColors.Grey.Medium);
                    });

                    // Derecha: Título y Folio en caja suave
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("COMPROBANTE DE ABONO").Bold().FontSize(13).FontColor("#0EA5E9");

                        col.Item().PaddingTop(4).Container()
                            .Background("#E0F2FE")
                            .PaddingHorizontal(10)
                            .PaddingVertical(4)
                            .Element(c => c.Text($"Folio: AB-{_abono.Id:D6}").Bold().FontSize(10).FontColor("#0369A1"));

                        col.Item().PaddingTop(4).Text($"Fecha de emisión:\n{_abono.FechaPago:dd MMM. yyyy - hh:mm tt}").FontSize(8).FontColor(QColors.Grey.Medium);
                    });
                });

                headerCol.Item().PaddingTop(10).LineHorizontal(1).LineColor(ColorBordeCaja);
            });
        }

        private void ConstruirContenido(QuestPDF.Infrastructure.IContainer container)
        {
            container.PaddingVertical(10).Column(col =>
            {
                col.Spacing(8);

                // 1. Sección: Datos del Cliente
                col.Item().Container()
                    .Border(1).BorderColor(ColorBordeCaja)
                    .Background(ColorFondoCaja)
                    .CornerRadius(6)
                    .Padding(8)
                    .Column(c =>
                    {
                        c.Item().Background(ColorPrimario).PaddingHorizontal(6).PaddingVertical(2).Width(110)
                            .AlignCenter().Text("Datos del cliente").Bold().FontSize(9).FontColor(QColors.White);
                        c.Item().PaddingTop(6);

                        c.Item().Row(r =>
                        {
                            r.ConstantItem(70).Text("Nombre:").Bold().FontColor(ColorTextoOscuro);
                            r.RelativeItem().Text(_evento.nombre_cliente ?? "N/A");
                        });
                        c.Item().PaddingTop(2);
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(70).Text("Teléfono:").Bold().FontColor(ColorTextoOscuro);
                            r.RelativeItem().Text(_evento.telefono_cliente ?? "N/A");
                        });
                    });

                // 2. Sección: Datos del Evento
                col.Item().Container()
                    .Border(1).BorderColor(ColorBordeCaja)
                    .Background(ColorFondoCaja)
                    .CornerRadius(6)
                    .Padding(8)
                    .Column(c =>
                    {
                        c.Item().Background(ColorPrimario).PaddingHorizontal(6).PaddingVertical(2).Width(110)
                            .AlignCenter().Text("Datos del evento").Bold().FontSize(9).FontColor(QColors.White);
                        c.Item().PaddingTop(6);

                        c.Item().Row(r =>
                        {
                            r.ConstantItem(110).Text("Evento:").Bold().FontColor(ColorTextoOscuro);
                            r.RelativeItem().Text(_evento.tipo_evento ?? "Evento Infantil");
                        });
                        c.Item().PaddingTop(2);
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(110).Text("Fecha:").Bold().FontColor(ColorTextoOscuro);
                            r.RelativeItem().Text(_evento.fecha_evento.ToString("dd MMM. yyyy"));
                        });
                        c.Item().PaddingTop(2);
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(110).Text("Número de invitados:").Bold().FontColor(ColorTextoOscuro);
                            r.RelativeItem().Text(_evento.numero_personas.ToString());
                        });
                    });

                // 3. Sección: Detalle de Abono
                decimal saldoPendiente = _saldoAnterior - (decimal)_abono.Monto;
                if (saldoPendiente < 0) saldoPendiente = 0;

                col.Item().Container()
                    .Border(1).BorderColor(ColorBordeCaja)
                    .Background(ColorFondoCaja)
                    .CornerRadius(6)
                    .Padding(8)
                    .Column(c =>
                    {
                        c.Item().Background(ColorPrimario).PaddingHorizontal(6).PaddingVertical(2).Width(110)
                            .AlignCenter().Text("Detalle de abono").Bold().FontSize(9).FontColor(QColors.White);
                        c.Item().PaddingTop(6);

                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Costo total del evento:");
                            r.ConstantItem(100).AlignRight().Text($"${_evento.precio:F2}").Bold();
                        });
                        c.Item().PaddingTop(2);
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Saldo anterior:");
                            r.ConstantItem(100).AlignRight().Text($"${_saldoAnterior:F2}");
                        });
                        c.Item().PaddingTop(2);
                        // Fila resaltada en turquesa claro para el monto del abono (idéntico a la referencia)
                        c.Item().Background(ColorFondoResaltado).Padding(4).Row(r =>
                        {
                            r.RelativeItem().Text("Monto del abono:").Bold().FontColor("#00796B");
                            r.ConstantItem(100).AlignRight().Text($"${_abono.Monto:F2}").Bold().FontColor("#00796B");
                        });
                        c.Item().PaddingTop(2);
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Saldo pendiente:");
                            r.ConstantItem(100).AlignRight().Text($"${saldoPendiente:F2}").Bold().FontColor(QColors.Red.Darken2);
                        });
                    });

                // 4. Sección: Método de pago y Observaciones
                col.Item().Container()
                    .Border(1).BorderColor(ColorBordeCaja)
                    .Background(ColorFondoCaja)
                    .CornerRadius(6)
                    .Padding(8)
                    .Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Background(ColorPrimario).PaddingHorizontal(6).PaddingVertical(2).Width(110)
                                .AlignCenter().Text("Método de pago").Bold().FontSize(9).FontColor(QColors.White);
                            c.Item().PaddingTop(6);

                            c.Item().Row(r =>
                            {
                                r.ConstantItem(90).Text("Método de pago:").Bold();
                                r.RelativeItem().Text(_abono.MetodoPago ?? "Efectivo");
                            });
                            c.Item().PaddingTop(2);
                            c.Item().Row(r =>
                            {
                                r.ConstantItem(90).Text("Referencia:").Bold();
                                r.RelativeItem().Text($"ABONO-{_abono.Id}");
                            });
                        });

                        row.ConstantItem(15); // Espaciador vertical central

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().PaddingTop(18); // Alinear visualmente con las etiquetas
                            c.Item().Row(r =>
                            {
                                r.ConstantItem(90).Text("Observaciones:").Bold();
                                r.RelativeItem().Text("Pago registrado correctamente en el sistema Gitos Fiesta.").FontSize(8).Italic();
                            });
                        });
                    });
            });
        }

        private void ConstruirPieDePagina(QuestPDF.Infrastructure.IContainer container)
        {
            container.Column(col =>
            {
                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(ColorBordeCaja);
                col.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Text("Gitos Fiestas").FontSize(8).Bold().FontColor(QColors.Grey.Medium);
                    row.RelativeItem().AlignCenter().Text("811 234 5678").FontSize(8).FontColor(QColors.Grey.Medium);
                    row.RelativeItem().AlignRight().Text("gitosfiestas@gmail.com").FontSize(8).FontColor(QColors.Grey.Medium);
                });
            });
        }
    }
}