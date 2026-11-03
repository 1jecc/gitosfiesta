using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using gitosfiesta.Models;
using System.IO;
using QColors = QuestPDF.Helpers.Colors;

namespace gitosfiesta;

public class AbonoPdfService
{
    private readonly EventoModel _evento;
    private readonly AbonoModel _abono;
    private readonly decimal _saldoAnterior;

    private static readonly string ColorPrimario = "#19D3C5";
    private static readonly string ColorPrimarioClaro = "#E8FCFA";
    private static readonly string ColorBorde = "#CDEEEB";
    private static readonly string ColorTexto = "#425466";
    private static readonly string ColorTitulo = "#14B8A6";

    public AbonoPdfService(
        EventoModel evento,
        AbonoModel abono,
        decimal saldoAnterior)
    {
        _evento = evento;
        _abono = abono;
        _saldoAnterior = saldoAnterior;
    }

    public byte[] GenerarPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(18);

                page.DefaultTextStyle(x =>
                    x.FontSize(9)
                     .FontColor(ColorTexto));

                page.Header().Element(ConstruirEncabezado);
                page.Content().Element(ConstruirContenido);
                page.Footer().Element(ConstruirFooter);
            });
        }).GeneratePdf();
    }

    private void ConstruirEncabezado(QuestPDF.Infrastructure.IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    if (File.Exists("imagen3.png"))
                    {
                        col.Item()
                            .Width(180)
                            .Image("imagen3.png");
                    }
                    else
                    {
                        col.Item()
                            .Text("GITOS FIESTA")
                            .Bold()
                            .FontSize(22)
                            .FontColor(ColorPrimario);
                    }
                });


                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item()
                        .AlignRight()
                        .Text("COMPROBANTE DE ABONO")
                        .Bold()
                        .FontSize(14)
                        .FontColor(ColorTitulo);

                    col.Item()
                        .PaddingTop(8)
                        .AlignRight()
                        .Background(ColorPrimarioClaro)
                        .Padding(8)
                        .CornerRadius(15)
                        .Text($"Folio: AB-{_abono.Id:D6}")
                        .Bold()
                        .FontColor("#0F766E");

                    col.Item()
                        .PaddingTop(8)
                        .AlignRight()
                        .Text($"Fecha de emisión:\n{_abono.FechaPago:dd MMM yyyy hh:mm tt}")
                        .FontSize(8);
                });
            });

            column.Item()
                .PaddingTop(10)
                .LineHorizontal(1)
                .LineColor(ColorBorde);
        });
    }

    private void ConstruirContenido(QuestPDF.Infrastructure.IContainer container)
    {
        decimal saldoPendiente = _saldoAnterior - (decimal)_abono.Monto;

        if (saldoPendiente < 0)
            saldoPendiente = 0;

        container.PaddingTop(12)
            .Column(col =>
            {
                col.Spacing(10);

                CrearDatosCliente(col);
                CrearDatosEvento(col);
                CrearDetalleAbono(col, saldoPendiente);
                CrearMetodoPago(col);
            });
    }

    private void CrearDatosCliente(ColumnDescriptor col)
    {
        col.Item()
            .PaddingTop(-80)
            .Element(c =>
            {
                Tarjeta(c, "Datos del cliente", body =>
                {

                    body.Item().Row(r =>
                    {
                        r.ConstantItem(90).Text("Nombre:");
                        r.RelativeItem().Text(_evento.nombre_cliente ?? "N/A");
                    });

                    body.Item().PaddingTop(4);

                    body.Item().Row(r =>
                    {
                        r.ConstantItem(90).Text("Teléfono:");
                        r.RelativeItem().Text(_evento.telefono_cliente ?? "N/A");
                    });

                    body.Item().PaddingTop(4);

                    body.Item().Row(r =>
                    {
                        r.ConstantItem(90).Text("Correo:");
                        r.RelativeItem().Text("cliente@gitosfiesta.com");
                    });
                });
            });
    }

    private void CrearDatosEvento(ColumnDescriptor col)
    {
        col.Item()
            .Element(c =>
            {
                Tarjeta(c, "Datos del evento", body =>
                {
                    body.Item().PaddingTop(8);

                    body.Item().Row(r =>
                    {
                        r.ConstantItem(120).Text("Evento:");
                        r.RelativeItem().Text(_evento.tipo_evento);
                    });

                    body.Item().PaddingTop(4);

                    body.Item().Row(r =>
                    {
                        r.ConstantItem(120).Text("Fecha:");
                        r.RelativeItem().Text(_evento.fecha_evento.ToString("dd MMM yyyy"));
                    });

                    body.Item().PaddingTop(4);

                    body.Item().Row(r =>
                    {
                        r.ConstantItem(120).Text("Horario:");
                        r.RelativeItem().Text(_evento.hora_evento.ToString(@"hh\:mm"));
                    });

                    body.Item().PaddingTop(4);

                    body.Item().Row(r =>
                    {
                        r.ConstantItem(120).Text("Número de invitados:");
                        r.RelativeItem().Text(_evento.numero_personas.ToString());
                    });
                });
            });
    }

    private void CrearDetalleAbono(
        ColumnDescriptor col,
        decimal saldoPendiente)
    {
        col.Item()
            .Element(c =>
            {
                Tarjeta(c, "Detalle de abono", body =>
                {
                    body.Item().PaddingTop(8);

                    body.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Costo total del evento:");
                        r.ConstantItem(120)
                            .AlignRight()
                            .Text($"${_evento.precio:N2}");
                    });

                    body.Item().PaddingTop(6);

                    body.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Saldo anterior:");
                        r.ConstantItem(120)
                            .AlignRight()
                            .Text($"${_saldoAnterior:N2}");
                    });

                    body.Item().PaddingTop(6);

                    body.Item()
                        .Background(ColorPrimarioClaro)
                        .CornerRadius(8)
                        .Padding(6)
                        .Row(r =>
                        {
                            r.RelativeItem()
                                .Text("Monto del abono:")
                                .Bold()
                                .FontColor("#0F766E");

                            r.ConstantItem(120)
                                .AlignRight()
                                .Text($"${_abono.Monto:N2}")
                                .Bold()
                                .FontColor("#0F766E");
                        });

                    body.Item().PaddingTop(6);

                    body.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Saldo pendiente:");

                        r.ConstantItem(120)
                            .AlignRight()
                            .Text($"${saldoPendiente:N2}")
                            .Bold()
                            .FontColor(QColors.Red.Darken2);
                    });
                });
            });
    }

    private void CrearMetodoPago(ColumnDescriptor col)
    {
        col.Item()
            .Element(c =>
            {
                Tarjeta(c, "Método de pago", body =>
                {
                    body.Item().PaddingTop(8);

                    body.Item().Row(r =>
                    {
                        r.ConstantItem(120).Text("Método:");
                        r.RelativeItem().Text(_abono.MetodoPago ?? "Efectivo");
                    });

                    body.Item().PaddingTop(4);

                    body.Item().Row(r =>
                    {
                        r.ConstantItem(120).Text("Referencia:");
                        r.RelativeItem().Text($"ABONO-{_abono.Id}");
                    });
                });
            });
    }

    private void Tarjeta(
        QuestPDF.Infrastructure.IContainer container,
        string titulo,
        Action<ColumnDescriptor> contenido)
    {
        container
            .Border(0.5f)
            .BorderColor(ColorBorde)
            .CornerRadius(12)
            .Padding(12)
            .Column(col =>
            {
                col.Item()
                    .Background(ColorPrimarioClaro)
                    .PaddingHorizontal(14)
                    .PaddingVertical(5)
                    .CornerRadius(20)
                    .AlignLeft()
                    .Text(titulo)
                    .Bold()
                    .FontColor("#0F766E");

                contenido(col);
            });
    }

    private void ConstruirFooter(QuestPDF.Infrastructure.IContainer container)
    {
        container.Column(col =>
        {
            col.Item()
                .LineHorizontal(1)
                .LineColor(ColorBorde);

            col.Item()
                .PaddingTop(10)
                .Row(row =>
                {
                    row.RelativeItem()
                        .AlignCenter()
                        .Text("Gitos Fiestas");

                    row.ConstantItem(1)
                        .Background(ColorBorde);

                    row.RelativeItem()
                        .AlignCenter()
                        .Text("811 234 5678");

                    row.ConstantItem(1)
                        .Background(ColorBorde);

                    row.RelativeItem()
                        .AlignCenter()
                        .Text("gitosfiestas@gmail.com");
                });
        });
    }
}