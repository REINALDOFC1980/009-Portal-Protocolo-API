using Portal.Protocolos.API.Models.Atendimento.Ressarcimento;
using Portal.Protocolos.API.Services.Atendimento;
using Portal.Protocolos.API.Services.Atendimento.Policies;
using Portal.Protocolos.Shared.Constants;
using Portal.Protocolos.Shared.Exceptions;

namespace Portal.Protocolos.API.Service.Atendimento;

public sealed class RessarcimentoService
{
    private readonly ProcessoService _processoService;

    public RessarcimentoService(
        ProcessoService processoService)
    {
        _processoService = processoService;
    }

    public async Task<long> CriarAsync(
        int usuarioId,
        RessarcimentoCreateRequest request)
    {
        var contexto = new ContextoServico
        {
            Auto = request.Auto
        };

        var policy = new RessarcimentoPolicy();

        var resultado =
            policy.Avaliar(contexto);

        if (!resultado.Disponivel)
        {
            throw new BusinessException(
                resultado.Motivo ??
                "Não é possível abrir solicitação de ressarcimento para este AIT.");
        }

        var processo =
            await _processoService.CriarAsync(
                usuarioId,
                ServicoConstants.Ressarcimento,
                SituacaoProcessoConstants.AguardandoTriagem,
                request.Auto);

        return processo.ProcessoId;
    }
}
