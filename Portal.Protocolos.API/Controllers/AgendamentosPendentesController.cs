using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Services.Atendimento;

namespace Portal.Protocolos.API.Controllers;

[ApiController]
[Authorize(Policy = "AtendimentoInterno")]
[Route("api/atendimento/agendamentos/pendentes")]
public sealed class AgendamentosPendentesController : ControllerBase
{
    private readonly AgendamentosPendentesService _service;

    public AgendamentosPendentesController(AgendamentosPendentesService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Consultar(
        [FromQuery] string? cpf, [FromQuery] string? numeroAit)
    {
        return Ok(await _service.ConsultarAsync(cpf, numeroAit));
    }
}
