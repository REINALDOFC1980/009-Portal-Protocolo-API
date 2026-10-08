using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Models.Atendimento.Defesa;
using Portal.Protocolos.API.Service.Atendimento;
using Portal.Protocolos.API.Services.Atendimento;

namespace Portal.Protocolos.API.Controllers;

[ApiController]
[Authorize(Policy = "AtendimentoInterno")]
[Route("api/atendimento/defesas")]
public sealed class DefesaPresencialController : ControllerBase
{
    private readonly DefesaPresencialService _service;

    public DefesaPresencialController(DefesaPresencialService service)
    {
        _service = service;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Criar([FromForm] DefesaPresencialFormRequest form)
    {
        if (!int.TryParse(User.FindFirst("UsuarioId")?.Value, out var atendenteId))
            return Unauthorized(new { message = "Atendente não identificado." });

        var resultado = await _service.CriarAsync(atendenteId, form);
        return Ok(resultado);
    }
}
