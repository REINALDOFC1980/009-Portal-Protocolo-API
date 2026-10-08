using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Services.Atendimento;

namespace Portal.Protocolos.API.Controllers;

[ApiController]
[Authorize(Policy = "AtendimentoInterno")]
[Route("api/atendimento/processos")]
public sealed class ProcessosPresenciaisController : ControllerBase
{
    private readonly ProcessoPresencialDetalheService _service;

    public ProcessosPresenciaisController(ProcessoPresencialDetalheService service)
    {
        _service = service;
    }

    [HttpGet("{processoId:long}")]
    public async Task<IActionResult> Obter(long processoId)
    {
        var detalhe = await _service.ObterAsync(processoId);
        if (detalhe is null)
            return NotFound(new { message = "Processo presencial não encontrado." });

        return Ok(detalhe);
    }
}
