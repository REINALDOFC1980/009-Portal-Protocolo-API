using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Models.Atendimento.Jari;
using Portal.Protocolos.API.Services.Atendimento;

namespace Portal.Protocolos.API.Controllers;

[ApiController]
[Authorize(Policy = "AtendimentoInterno")]
[Route("api/atendimento/jari")]
public sealed class JariPresencialController : ControllerBase
{
    private readonly JariPresencialService _service;

    public JariPresencialController(JariPresencialService service)
    {
        _service = service;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Criar([FromForm] JariPresencialFormRequest form)
    {
        if (!int.TryParse(User.FindFirst("UsuarioId")?.Value, out var atendenteId))
        {
            return Unauthorized(new
            {
                message = "Atendente não identificado."
            });
        }

        var resultado = await _service.CriarAsync(atendenteId, form);
        return Ok(resultado);
    }
}
