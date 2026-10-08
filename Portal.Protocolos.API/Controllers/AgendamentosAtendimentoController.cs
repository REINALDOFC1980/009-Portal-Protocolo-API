using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Services.Atendimento;

namespace Portal.Protocolos.API.Controllers;

[ApiController]
[Authorize(Policy = "AtendimentoInterno")]
[Route("api/atendimento/agendamentos")]
public sealed class AgendamentosAtendimentoController : ControllerBase
{
    private readonly AgendamentoAtendimentoService _service;

    public AgendamentosAtendimentoController(AgendamentoAtendimentoService service)
    {
        _service = service;
    }

    [HttpGet("{agendamentoId}/ait")]
    public async Task<IActionResult> ObterAit(
        string agendamentoId, CancellationToken cancellationToken)
    {
        var resultado = await _service.ObterAitAsync(agendamentoId, cancellationToken);
        if (resultado.CodigoErro == 404) return NotFound(resultado.MensagemErro);
        if (resultado.CodigoErro == 502) return StatusCode(502, resultado.MensagemErro);
        return Ok(resultado.Dados);
    }

    [HttpGet]
    public async Task<IActionResult> Consultar(
        [FromQuery] string? cpf, [FromQuery] string? numeroAit)
    {
        if (string.IsNullOrWhiteSpace(cpf) && string.IsNullOrWhiteSpace(numeroAit))
            return BadRequest("Informe o CPF ou o número do AIT.");

        return Ok(await _service.ConsultarAsync(cpf, numeroAit));
    }
}
