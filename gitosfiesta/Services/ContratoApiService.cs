using System.Net.Http.Json;
using gitosfiesta.Models;

namespace gitosfiesta.Services;

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

public sealed class ContratoApiService
{
    private readonly HttpClient _httpClient;

    public ContratoApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<byte[]> GenerarPdfAsync(EventoModel evento, IEnumerable<MiscModel> terminos, string? firmaBase64)
    {
        var request = new ContratoPdfRequest
        {
            EventoId = evento.Id,
            Folio = evento.folio,
            FechaContrato = evento.fecha_contrato,
            NombreCliente = evento.nombre_cliente,
            TelefonoCliente = evento.telefono_cliente,
            TipoEvento = evento.tipo_evento,
            FechaEvento = evento.fecha_evento,
            HoraEvento = evento.hora_evento,
            NumeroPersonas = evento.numero_personas,
            Menu = evento.menu,
            Precio = evento.precio,
            Anticipo = evento.anticipo,
            Saldo = evento.Saldo,
            Promocion = evento.promocion,
            Observaciones = evento.observaciones,
            FirmaBase64 = firmaBase64,
            Terminos = terminos.Select(x => $"{x.value1}.- {x.value2}").ToList()
        };

        using var response = await _httpClient.PostAsJsonAsync($"api/contratos/{evento.Id}/pdf", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
}
