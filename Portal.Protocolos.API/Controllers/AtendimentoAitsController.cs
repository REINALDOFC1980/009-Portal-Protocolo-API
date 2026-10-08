using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Service.Atendimento;
using Portal.Protocolos.API.Services.Atendimento;

namespace Portal.Protocolos.API.Controllers;

[ApiController]
[Authorize(Policy = "AtendimentoInterno")]
[Route("api/atendimento/aits")]
public sealed class AtendimentoAitsController : ControllerBase
{
    private readonly AtendimentoAitService _service;

    public AtendimentoAitsController(
        AtendimentoAitService service)
    {
        _service = service;
    }

    [HttpGet("{numeroAuto}")]
    public async Task<IActionResult> Consultar(
        string numeroAuto,
        CancellationToken cancellationToken)
    {
        var auto = await _service.ConsultarAsync(
            numeroAuto,
            cancellationToken);

        if (auto is null)
        {
            return NotFound(new
            {
                message = "AIT não encontrado no SITRAN."
            });
        }

        return Ok(auto);
    }

    [HttpGet("{numeroAuto}/servicos")]
    public async Task<IActionResult> ConsultarServicosDisponiveis(
        string numeroAuto,
        CancellationToken cancellationToken)
    {
        var resultado = await _service.ConsultarComServicosAsync(
            numeroAuto,
            cancellationToken);

        if (resultado is null)
        {
            return NotFound(new
            {
                message = "AIT não encontrado no SITRAN."
            });
        }

        return Ok(resultado);
    }
}
