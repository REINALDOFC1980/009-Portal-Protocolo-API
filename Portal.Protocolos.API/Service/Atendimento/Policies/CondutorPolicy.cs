using Portal.Protocolos.Shared.Constants;

namespace Portal.Protocolos.API.Services.Atendimento.Policies;

public sealed class CondutorPolicy : IServicoPolicy
{
    public short ServicoId =>
        ServicoConstants.Condutor;

    public ResultadoPolicy Avaliar(
        ContextoServico contexto)
    {
        if (contexto.PossuiApresentacaoCondutor)
        {
            return new ResultadoPolicy
            {
                Disponivel = false,
                Motivo =
                    "Já existe processo de apresentação de condutor para este auto."
            };
        }

        var elegibilidade =
            contexto.ElegibilidadeSitran?.Ritos.Condutor;

        if (elegibilidade is null)
        {
            return new ResultadoPolicy
            {
                Disponivel = false,
                Motivo =
                    "Não foi possível consultar a elegibilidade para apresentação de condutor."
            };
        }

        if (!elegibilidade.Permitido)
        {
            return new ResultadoPolicy
            {
                Disponivel = false,
                Motivo =
                    elegibilidade.Motivo ??
                    "O auto não está elegível para apresentação de condutor."
            };
        }

        return new ResultadoPolicy
        {
            Disponivel = true,

            Observacao =
                contexto.Auto.PrazoApresentacaoCondutor.HasValue
                    ? $"Prazo para apresentação: {contexto.Auto.PrazoApresentacaoCondutor.Value:dd/MM/yyyy}."
                    : null
        };
    }
}
