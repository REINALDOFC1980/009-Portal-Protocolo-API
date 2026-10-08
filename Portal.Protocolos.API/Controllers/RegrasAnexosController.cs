using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Models.Atendimento.Anexo;
using Portal.Protocolos.API.Services.Atendimento;

namespace Portal.Protocolos.API.Controllers;

[ApiController]
[Authorize(Policy = "AtendimentoInterno")]
[Route("api/atendimento/regras-anexos")]
public sealed class RegrasAnexosController : ControllerBase
{
    private readonly RegraAnexoService _service;

    public RegrasAnexosController(RegraAnexoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] short servicoId,
        [FromQuery] string? origem,
        [FromQuery] string? tipoSolicitante,
        [FromQuery] bool incluirInativos = false)
    {
        if (incluirInativos && !EhGestor()) return Forbid();
        return Ok(await _service.ListarAsync(servicoId, origem, tipoSolicitante, incluirInativos));
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] RegraAnexoSalvarRequest request)
    {
        if (!EhGestor()) return Forbid();
        try
        {
            var id = await _service.CriarAsync(request);
            return Ok(new { regraAnexoId = id });
        }
        catch (RegraAnexoDuplicadaException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] RegraAnexoSalvarRequest request)
    {
        if (!EhGestor()) return Forbid();
        try
        {
            var atualizado = await _service.AtualizarAsync(id, request);
            return atualizado ? Ok(new { regraAnexoId = id }) : NotFound();
        }
        catch (RegraAnexoDuplicadaException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private bool EhGestor() =>
        User.IsInRole("SUPERVISOR") || User.IsInRole("ADMINISTRADOR");
}
