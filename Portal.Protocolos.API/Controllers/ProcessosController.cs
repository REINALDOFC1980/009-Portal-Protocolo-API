using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Services.Integracao;

namespace Portal.Protocolos.API.Controllers;

[Authorize]
[ApiController]
[Route("api/processos")]
public sealed class ProcessosController : ControllerBase
{
    private readonly ProcessoRepository _processoRepository;
    private readonly SitranApiClient _sitranApiClient;

    public ProcessosController(
        ProcessoRepository processoRepository,
        SitranApiClient sitranApiClient)
    {
        _processoRepository = processoRepository;
        _sitranApiClient = sitranApiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        CancellationToken cancellationToken)
    {
        var usuarioIdClaim =
            User.FindFirst("UsuarioId")?.Value;

        var documentoClaim =
            User.FindFirst("Documento")?.Value;

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

        var documento =
            SomenteNumeros(
                documentoClaim);

        if (string.IsNullOrWhiteSpace(
                documento))
        {
            return Unauthorized(new
            {
                message =
                    "Documento do usuário não identificado."
            });
        }

        var processos =
            await _processoRepository
                .ListarPorUsuarioAsync(
                    usuarioId,
                    documento);

        foreach (var processo in processos)
        {
            // APRESENTAÇÃO DE CONDUTOR
            if (processo.ServicoCodigo.Equals(
                    "condutor",
                    StringComparison.OrdinalIgnoreCase))
            {
                var condutor =
                    await _sitranApiClient
                        .ObterCondutorAsync(
                            processo.NumeroAuto,
                            cancellationToken);

                if (condutor != null)
                {
                    processo.RetornoDetran =
                        condutor.RetornoDetran;

                    if (!string.IsNullOrWhiteSpace(
                            condutor.RetornoDetran))
                    {
                        processo.Situacao =
                            condutor
                                .RetornoDetran
                                .Trim();
                    }
                }

                continue;
            }

            // DEFESA
            if (processo.ServicoCodigo.Equals(
                    "defesa",
                    StringComparison.OrdinalIgnoreCase))
            {
                var defesa =
                    await _sitranApiClient
                        .ObterDefesaAsync(
                            processo.NumeroAuto,
                            cancellationToken);

                if (defesa?.Resultado == null)
                {
                    processo.Situacao =
                        "Aguardando Julgamento";

                    continue;
                }

                var codigo =
                    defesa.Resultado
                        .Codigo?
                        .Trim()
                        .ToUpperInvariant();

                processo.CodigoResultado =
                    codigo;

                processo.Resultado =
                    codigo == "X"
                        ? null
                        : defesa.Resultado.Descricao;

                processo.DataJulgamento =
                    codigo == "X"
                        ? null
                        : defesa.DataResultado;

                processo.DataPublicacaoResultado =
                    codigo == "X"
                        ? null
                        : defesa.DataPublicacao;

                processo.Situacao =
                    MapearSituacaoJulgamento(
                        codigo);

                continue;
            }

            // JARI
            if (processo.ServicoCodigo.Equals(
                    "jari",
                    StringComparison.OrdinalIgnoreCase))
            {
                var jari =
                    await _sitranApiClient
                        .ObterJariAsync(
                            processo.NumeroAuto,
                            cancellationToken);

                if (jari?.Resultado == null)
                {
                    processo.Situacao =
                        "Aguardando Julgamento";

                    continue;
                }

                var codigo =
                    jari.Resultado
                        .Codigo?
                        .Trim()
                        .ToUpperInvariant();

                processo.CodigoResultado =
                    codigo;

                processo.Resultado =
                    codigo == "X"
                        ? null
                        : jari.Resultado.Descricao;

                processo.DataJulgamento =
                    codigo == "X"
                        ? null
                        : jari.DataResultado;

                processo.DataPublicacaoResultado =
                    codigo == "X"
                        ? null
                        : jari.DataPublicacao;

                processo.Situacao =
                    MapearSituacaoJulgamento(
                        codigo);
            }
        }

        return Ok(processos);
    }
    private static string MapearSituacaoJulgamento(
    string? codigoResultado)
    {
        return codigoResultado?.Trim().ToUpperInvariant() switch
        {
            "X" => "Aguardando Julgamento",
            "D" => "Deferido",
            "I" => "Indeferido",
            _ => "Aguardando Julgamento"
        };
    }

    private static string SomenteNumeros(
    string? valor)
    {
        if (string.IsNullOrWhiteSpace(
                valor))
        {
            return string.Empty;
        }

        return new string(
            valor
                .Where(char.IsDigit)
                .ToArray());
    }
}
