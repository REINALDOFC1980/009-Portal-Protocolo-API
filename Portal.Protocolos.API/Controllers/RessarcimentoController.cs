using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Models.Atendimento.Ressarcimento;
using Portal.Protocolos.API.Service.Atendimento;

namespace Portal.Protocolos.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class RessarcimentoController : ControllerBase
{
    private readonly RessarcimentoService _service;

    public RessarcimentoController(
        RessarcimentoService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] RessarcimentoCreateRequest request)
    {
        var usuarioIdClaim =
            User.FindFirst("UsuarioId")?.Value;

        if (!int.TryParse(
                usuarioIdClaim,
                out var usuarioId))
        {
            return Unauthorized();
        }

        var processoId =
            await _service.CriarAsync(
                usuarioId,
                request);

        return Ok(new
        {
            ProcessoId = processoId,
            Message = "Solicitação de ressarcimento registrada com sucesso."
        });
    }
}
