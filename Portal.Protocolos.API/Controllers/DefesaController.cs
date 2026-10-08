using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Models.Atendimento.Defesa;
using Portal.Protocolos.API.Services.Atendimento;
using Portal.Protocolos.Shared.Constants;
using Portal.Protocolos.Shared.Exceptions;

namespace Portal.Protocolos.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class DefesaController : ControllerBase
{
    private readonly DefesaService _service;

    public DefesaController(
        DefesaService service)
    {
        _service = service;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Criar(
        [FromForm] DefesaCreateFormRequest form)
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
                form.NumeroAuto))
        {
            throw new BusinessException(
                "O auto de infração não foi informado.");
        }

        var processo =
            await _service.CriarAsync(
                usuarioId,
                documentoUsuario,
                form.NumeroAuto,
                form.Arquivos,
                form.TiposDocumento);

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
                    ? "Processo recebido com sucesso. A integração está pendente."
                    : "Defesa registrada com sucesso."
        });
    }

    [HttpPost("{processoId:long}/reprocessar")]
    public async Task<IActionResult> Reprocessar(
        long processoId)
    {
        var usuarioIdClaim =
            User.FindFirst("UsuarioId")?.Value;

        if (!int.TryParse(
                usuarioIdClaim,
                out var usuarioId))
        {
            return Unauthorized(new
            {
                message =
                    "Usuário não identificado."
            });
        }

        var processo =
            await _service.ReprocessarAsync(
                processoId,
                usuarioId);

        return Ok(new
        {
            processoId =
                processo.ProcessoId,

            protocolo =
                processo.NumeroProcesso,

            situacaoProcessoId =
                SituacaoProcessoConstants
                    .AguardandoTriagem,

            message =
                "Integração confirmada com sucesso."
        });
    }

    [HttpGet("{processoId:long}")]
    public async Task<IActionResult> Obter(
        long processoId)
    {
        var usuarioIdClaim =
            User.FindFirst("UsuarioId")?.Value;

        if (!int.TryParse(
                usuarioIdClaim,
                out var usuarioId))
        {
            return Unauthorized(new
            {
                message =
                    "Usuário não identificado."
            });
        }

        var defesa =
            await _service.ObterAsync(
                processoId,
                usuarioId);

        return Ok(defesa);
    }
}
