using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Services.Integracao;

namespace Portal.Protocolos.API.Controllers;

[Authorize]
[ApiController]
[Route("api/infracoes")]
public sealed class InfracoesController : ControllerBase
{
    private readonly AutoInfracaoService _autoInfracaoService;
    private readonly SitranApiClient _sitranApiClient;

    public InfracoesController(
        AutoInfracaoService autoInfracaoService,
        SitranApiClient sitranApiClient)
    {
        _autoInfracaoService = autoInfracaoService;
        _sitranApiClient = sitranApiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var documento = User.FindFirst("Documento")?.Value;

        if (string.IsNullOrWhiteSpace(documento))
        {
            return Unauthorized(new
            {
                message = "Usuário não identificado."
            });
        }

        var autos =
            await _autoInfracaoService
                .ListarPorDocumentoAsync(documento);

        return Ok(autos);
    }

    [HttpGet("{numeroAuto}")]
    public async Task<IActionResult> Obter(
        string numeroAuto)
    {
        var documento = User.FindFirst("Documento")?.Value;

        if (string.IsNullOrWhiteSpace(documento))
        {
            return Unauthorized(new
            {
                message = "Usuário não identificado."
            });
        }

        var auto =
            await _autoInfracaoService
                .ObterPorNumeroAsync(
                    numeroAuto,
                    documento);

        if (auto is null)
        {
            return NotFound(new
            {
                message = "Auto de infração não encontrado."
            });
        }

        return Ok(auto);
    }

    [HttpGet("{numeroAuto}/elegibilidade")]
    public async Task<IActionResult> ObterElegibilidade(
        string numeroAuto)
    {
        var documento = User.FindFirst("Documento")?.Value;

        if (string.IsNullOrWhiteSpace(documento))
        {
            return Unauthorized(new
            {
                message = "Usuário não identificado."
            });
        }

        // Primeiro confirma que o AIT pertence
        // ao usuário autenticado.
        var auto =
            await _autoInfracaoService
                .ObterPorNumeroAsync(
                    numeroAuto,
                    documento);

        if (auto is null)
        {
            return NotFound(new
            {
                message = "Auto de infração não encontrado."
            });
        }

        var elegibilidade =
            await _sitranApiClient
                .ObterElegibilidadeAsync(numeroAuto);

        if (elegibilidade is null)
        {
            return NotFound(new
            {
                message = "Elegibilidade do AIT não encontrada."
            });
        }

        return Ok(elegibilidade);
    }
}
