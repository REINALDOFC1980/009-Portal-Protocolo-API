using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Models.Atendimento;
using Portal.Protocolos.API.Services.Atendimento;
using Portal.Protocolos.API.Services.Atendimento.Policies;
using Portal.Protocolos.API.Services.Integracao;

namespace Portal.Protocolos.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class ServicoController : ControllerBase
{
    private readonly ServicoDisponivelService _servicoDisponivelService;
    private readonly AutoInfracaoService _autoInfracaoService;

    public ServicoController(
        ServicoDisponivelService servicoDisponivelService,
        AutoInfracaoService autoInfracaoService)
    {
        _servicoDisponivelService = servicoDisponivelService;
        _autoInfracaoService = autoInfracaoService;
    }

    [HttpPost("avaliar")]
    public async Task<IActionResult> Avaliar(
        AvaliarServicoRequest request)
    {
        var documento = User.FindFirst("Documento")?.Value;

        if (string.IsNullOrWhiteSpace(documento))
        {
            return Unauthorized(new
            {
                message = "Usuário não identificado."
            });
        }

        if (string.IsNullOrWhiteSpace(request.NumeroAuto))
        {
            return BadRequest(new
            {
                message = "Número do auto é obrigatório."
            });
        }

        var numeroAuto = request.NumeroAuto.Trim();

        var auto =
            await _autoInfracaoService.ObterPorNumeroAsync( numeroAuto,documento);

        if (auto is null)
        {
            return NotFound(new
            {
                message = "Auto de infração não encontrado."
            });
        }

        var contexto =
            new ContextoServico
            {
                Auto = auto
            };

        var resultado =
            await _servicoDisponivelService.AvaliarAsync(contexto);

        return Ok(resultado);
    }
}
