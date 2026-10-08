using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Models.Atendimento.Condutor;
using Portal.Protocolos.API.Services.Atendimento;

namespace Portal.Protocolos.API.Controllers;

[ApiController]
[Authorize(Policy = "AtendimentoInterno")]
[Route("api/atendimento/condutor")]
public sealed class CondutorPresencialController : ControllerBase
{
    private readonly CondutorPresencialService _service;

    public CondutorPresencialController(
        CondutorPresencialService service)
    {
        _service = service;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Criar(
        [FromForm] CondutorPresencialFormRequest form)
    {
        if (!int.TryParse(
                User.FindFirst("UsuarioId")?.Value,
                out var atendenteId))
        {
            return Unauthorized(new
            {
                message = "Atendente não identificado."
            });
        }

        var resultado = await _service.CriarAsync(
            atendenteId,
            form);

        return Ok(resultado);
    }
}
