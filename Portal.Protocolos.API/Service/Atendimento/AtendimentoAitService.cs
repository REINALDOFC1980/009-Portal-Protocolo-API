using Portal.Protocolos.API.Models.Atendimento.Ait;
using Portal.Protocolos.API.Models.Integracao;
using Portal.Protocolos.API.Services.Integracao;
using Portal.Protocolos.Shared.Exceptions;
using System.Net;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class AtendimentoAitService
{
    private readonly SitranApiClient _sitran;
    private readonly ServicoDisponivelService _servicos;
    private readonly ResponsavelAitService _responsaveis;

    public AtendimentoAitService(
        SitranApiClient sitran,
        ServicoDisponivelService servicos,
        ResponsavelAitService responsaveis)
    {
        _sitran = sitran;
        _servicos = servicos;
        _responsaveis = responsaveis;
    }

    public async Task<AutoInfracaoResponse?> ConsultarAsync(
        string? numeroAuto,
        CancellationToken cancellationToken = default)
    {
        var numero = NormalizarNumeroAuto(numeroAuto);

        var auto = await _sitran.ObterAitAsync(
            numero,
            cancellationToken);

        if (auto is null)
            return null;

        if (!auto.NumeroAuto.Equals(
                numero,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new SitranApiException(
                HttpStatusCode.BadGateway,
                "AIT_DIVERGENTE",
                "O SITRAN retornou um AIT diferente do solicitado.");
        }

        return auto;
    }

    public async Task<AitAtendimentoDisponibilidadeResponse?>
        ConsultarComServicosAsync(
            string? numeroAuto,
            CancellationToken cancellationToken = default)
    {
        var auto = await ConsultarAsync(
            numeroAuto,
            cancellationToken);

        if (auto is null)
            return null;

        var responsavelAtual = await _responsaveis.ObterAtualAsync(
            auto,
            cancellationToken);

        var servicos = await _servicos.AvaliarAsync(
            new Policies.ContextoServico
            {
                Auto = auto
            });

        return new AitAtendimentoDisponibilidadeResponse
        {
            AutoInfracao = auto,
            ResponsavelAtual = responsavelAtual,
            Servicos = servicos
        };
    }

    private static string NormalizarNumeroAuto(string? numeroAuto)
    {
        var numero = (numeroAuto ?? string.Empty).Trim();

        if (numero.Length is < 3 or > 30)
            throw new BusinessException(
                "Informe um número de AIT válido.");

        return numero;
    }
}
