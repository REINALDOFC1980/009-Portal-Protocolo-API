using Portal.Protocolos.Shared.Constants;

namespace Portal.Protocolos.API.Services.Atendimento.Policies;

public sealed class RessarcimentoPolicy : IServicoPolicy
{
    public short ServicoId => ServicoConstants.Ressarcimento;

    public ResultadoPolicy Avaliar(ContextoServico contexto)
    {
        if (contexto.PossuiProcessoRessarcimento)
        {
            return new ResultadoPolicy
            {
                Disponivel = false,
                Motivo = "Já existe solicitação de ressarcimento para este auto."
            };
        }

        if (!contexto.PossuiPagamento)
        {
            return new ResultadoPolicy
            {
                Disponivel = false,
                Motivo = "Não foi localizado pagamento para este auto."
            };
        }

        if (!contexto.PossuiMotivoRessarcimento)
        {
            return new ResultadoPolicy
            {
                Disponivel = false,
                Motivo = "Não foi identificada condição para solicitação de ressarcimento."
            };
        }

        var alertas = new List<string>();

        if (contexto.PagamentoDuplicado)
        {
            alertas.Add(
                "Foi identificado mais de um pagamento para o auto.");
        }

        if (contexto.AutoCancelado)
        {
            alertas.Add(
                "O auto possui indicação de cancelamento após pagamento.");
        }

        return new ResultadoPolicy
        {
            Disponivel = true,
            Alertas = alertas
        };
    }
}
