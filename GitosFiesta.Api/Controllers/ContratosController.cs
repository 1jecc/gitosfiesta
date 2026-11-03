using GitosFiesta.Api.Models;
using GitosFiesta.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GitosFiesta.Api.Controllers;

[ApiController]
[Route("api/contratos")]
public sealed class ContratosController : ControllerBase
{
    private readonly ContratoPdfGenerator _generator = new();

    [HttpPost("{eventoId:long}/pdf")]
    public IActionResult GeneratePdf(long eventoId, [FromBody] ContratoPdfRequest request)
    {
        if (request.EventoId != 0 && request.EventoId != eventoId)
            return BadRequest("El identificador del evento no coincide.");

        request.EventoId = eventoId;
        var pdf = _generator.Generate(request);

        return File(pdf, "application/pdf", $"contrato-{eventoId}.pdf");
    }
}
