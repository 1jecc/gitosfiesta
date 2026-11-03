using GitosFiesta.Api.Models;
using GitosFiesta.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GitosFiesta.Api.Controllers;

[ApiController]
[Route("api/abonos")]
public sealed class AbonosController : ControllerBase
{
    private readonly AbonoPdfGenerator _generator = new();

    [HttpPost("{abonoId:long}/pdf")]
    public IActionResult GeneratePdf(long abonoId, [FromBody] AbonoPdfRequest request)
    {
        if (request.AbonoId != 0 && request.AbonoId != abonoId)
            return BadRequest("El identificador del abono no coincide.");

        request.AbonoId = abonoId;
        var pdf = _generator.Generate(request);

        return File(pdf, "application/pdf", $"abono-{abonoId}.pdf");
    }
}
