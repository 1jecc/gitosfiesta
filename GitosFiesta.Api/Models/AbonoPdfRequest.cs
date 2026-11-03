namespace GitosFiesta.Api.Models;

public sealed class AbonoPdfRequest
{
    public long EventoId { get; set; }
    public long AbonoId { get; set; }
    public string? FolioEvento { get; set; }
    public string? NombreCliente { get; set; }
    public string? TelefonoCliente { get; set; }
    public string? TipoEvento { get; set; }
    public DateTime FechaEvento { get; set; }
    public DateTime FechaPago { get; set; }
    public decimal Monto { get; set; }
    public decimal SaldoAnterior { get; set; }
    public string? MetodoPago { get; set; }
}
