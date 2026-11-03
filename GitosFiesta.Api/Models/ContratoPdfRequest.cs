namespace GitosFiesta.Api.Models;

public sealed class ContratoPdfRequest
{
    public long EventoId { get; set; }
    public string? Folio { get; set; }
    public DateTime FechaContrato { get; set; }
    public string? NombreCliente { get; set; }
    public string? TelefonoCliente { get; set; }
    public string? TipoEvento { get; set; }
    public DateTime FechaEvento { get; set; }
    public TimeSpan HoraEvento { get; set; }
    public int NumeroPersonas { get; set; }
    public string? Menu { get; set; }
    public decimal Precio { get; set; }
    public decimal Anticipo { get; set; }
    public decimal Saldo { get; set; }
    public string? Promocion { get; set; }
    public string? Observaciones { get; set; }
    public string? FirmaBase64 { get; set; }
    public List<string> Terminos { get; set; } = new();
}
