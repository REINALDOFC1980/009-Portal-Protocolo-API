using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Models.Atendimento.Condutor;
using Portal.Protocolos.API.Services.Atendimento;
using Portal.Protocolos.API.Services.Integracao;
using Portal.Protocolos.Shared.Constants;
using Portal.Protocolos.Shared.Exceptions;

namespace Portal.Protocolos.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class CondutorController : ControllerBase
{
    private readonly CondutorService _condutorService;
    private readonly AutoInfracaoService _autoInfracaoService;

    public CondutorController(
        CondutorService condutorService,
        AutoInfracaoService autoInfracaoService)
    {
        _condutorService =
            condutorService;

        _autoInfracaoService =
            autoInfracaoService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Criar(
        [FromForm] CondutorCreateFormRequest form)
    {
        var usuarioIdClaim =
            User.FindFirst("UsuarioId")?.Value;

        var documentoUsuario =
            User.FindFirst("Documento")?.Value;

        if (!int.TryParse(
                usuarioIdClaim,
                out var usuarioId) ||
            string.IsNullOrWhiteSpace(
                documentoUsuario))
        {
            return Unauthorized(new
            {
                message =
                    "Usuário não identificado."
            });
        }

        if (string.IsNullOrWhiteSpace(
                form.DocumentoCondutor))
        {
            throw new BusinessException(
                "O documento do condutor não foi informado.");
        }

        if (string.IsNullOrWhiteSpace(
                form.NumeroAuto))
        {
            throw new BusinessException(
                "O auto de infração não foi informado.");
        }

        var auto =
            await _autoInfracaoService
                .ObterPorNumeroAsync(
                    form.NumeroAuto,
                    documentoUsuario);

        if (auto == null)
        {
            throw new BusinessException(
                "Auto de infração não encontrado.");
        }

        var request =
            new CondutorCreateRequest
            {
                DocumentoCondutor =
                    form.DocumentoCondutor,

                NomeCondutor =
                    form.NomeCondutor,

                RegistroCnh =
                    form.RegistroCnh,

                UfCnh =
                    form.UfCnh,

                ModeloCnh =
                    form.ModeloCnh,

                Auto =
                    auto
            };

        var processo =
            await _condutorService
                .CriarAsync(
                    usuarioId,
                    request,
                    form.Arquivos,
                    form.TiposDocumento);

        return Ok(new
        {
            processoId =
                processo.ProcessoId,

            protocolo =
                processo.NumeroProcesso,

            message =
                "Apresentação de Condutor registrada com sucesso. Aguardando aceite do condutor indicado."
        });
    }

    [HttpGet("pendentes")]
    public async Task<IActionResult> ListarPendentes()
    {
        var documento =
            User.FindFirst("Documento")?.Value;

        if (string.IsNullOrWhiteSpace(
                documento))
        {
            return Unauthorized(new
            {
                message =
                    "Usuário não identificado."
            });
        }

        var processos =
            await _condutorService
                .ListarPendentesAsync(
                    documento);

        return Ok(processos);
    }

    [HttpPost("{processoCondutorId:long}/aceitar")]
    public async Task<IActionResult> Aceitar(long processoCondutorId)
    {
        var usuarioIdClaim =
            User.FindFirst("UsuarioId")?.Value;

        var documento =
            User.FindFirst("Documento")?.Value;

        if (!int.TryParse(
                usuarioIdClaim,
                out var usuarioId) ||
            string.IsNullOrWhiteSpace(
                documento))
        {
            return Unauthorized(new
            {
                message =
                    "Usuário não identificado."
            });
        }

        var processo =
            await _condutorService
                .AceitarAsync(
                    processoCondutorId,
                    usuarioId,
                    documento);

        var integracaoPendente =
            processo.SituacaoProcessoId ==
            SituacaoProcessoConstants
                .AguardandoRetornoExterno;

        return Ok(new
        {
            processoId =
                processo.ProcessoId,

            protocolo =
                processo.NumeroProcesso,

            situacaoProcessoId =
                processo.SituacaoProcessoId,

            message =
                integracaoPendente
                    ? "Aceite registrado com sucesso. A integração está pendente."
                    : "Apresentação de Condutor aceita com sucesso."
        });
    }

    [HttpPost("{processoCondutorId:long}/reprocessar")]
    public async Task<IActionResult> Reprocessar(
        long processoCondutorId)
    {
        var usuarioIdClaim =
            User.FindFirst("UsuarioId")?.Value;

        var documento =
            User.FindFirst("Documento")?.Value;

        if (!int.TryParse(
                usuarioIdClaim,
                out var usuarioId) ||
            string.IsNullOrWhiteSpace(
                documento))
        {
            return Unauthorized(new
            {
                message =
                    "Usuário não identificado."
            });
        }

        var processo =
            await _condutorService
                .ReprocessarAsync(
                    processoCondutorId,
                    usuarioId,
                    documento);

        return Ok(new
        {
            processoId =
                processo.ProcessoId,

            protocolo =
                processo.NumeroProcesso,

            situacaoProcessoId =
                processo.SituacaoProcessoId,

            message =
                "Integração confirmada com sucesso."
        });
    }

    [HttpPost("{processoCondutorId:long}/recusar")]
    public async Task<IActionResult> Recusar(long processoCondutorId, [FromBody] RecusarCondutorRequest request)
    {
        var usuarioIdClaim =
            User.FindFirst("UsuarioId")?.Value;

        var documento =
            User.FindFirst("Documento")?.Value;

        if (!int.TryParse(
                usuarioIdClaim,
                out var usuarioId) ||
            string.IsNullOrWhiteSpace(
                documento))
        {
            return Unauthorized(new
            {
                message =
                    "Usuário não identificado."
            });
        }

        await _condutorService
            .RecusarAsync(
                processoCondutorId,
                usuarioId,
                documento,
                request.Motivo);

        return Ok(new
        {
            message =
                "Processo recusado com sucesso."
        });
    }
}
