using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Services.Atendimento;

namespace Portal.Protocolos.API.Controllers;

[ApiController]
[Authorize(Policy = "AtendimentoInterno")]
[Route("api/atendimento/copia-processo")]
public sealed class CopiaProcessoController : ControllerBase
{
    private readonly CopiaProcessoService _service;

    public CopiaProcessoController(CopiaProcessoService service)
    {
        _service = service;
    }

    [HttpGet("{numeroProcesso}")]
    public async Task<IActionResult> Consultar(string numeroProcesso)
    {
        var processo = await _service.ConsultarAsync(numeroProcesso);

        if (processo is null)
        {
            return NotFound(new
            {
                message = "Processo não encontrado."
            });
        }

        return Ok(processo);
    }

    [HttpGet("anexos/{processoAnexoId:long}")]
    public async Task<IActionResult> ObterAnexo(long processoAnexoId)
    {
        var arquivo = await _service.ObterArquivoAsync(processoAnexoId);

        return File(
            arquivo.Conteudo,
            arquivo.ContentType);
    }
}
