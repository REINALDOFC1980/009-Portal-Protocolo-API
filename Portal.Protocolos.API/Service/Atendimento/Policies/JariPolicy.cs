using Portal.Protocolos.Shared.Constants;

namespace Portal.Protocolos.API.Services.Atendimento.Policies;

public sealed class JariPolicy : IServicoPolicy
{
    public short ServicoId =>
        ServicoConstants.Jari;

    public ResultadoPolicy Avaliar(
        ContextoServico contexto)
    {
        if (contexto.PossuiProcessoJari)
        {
            return new ResultadoPolicy
            {
                Disponivel = false,
                Motivo =
                    "Já existe recurso JARI protocolado para este auto."
            };
        }

        var elegibilidade =
            contexto.ElegibilidadeSitran?.Ritos.Jari;

        if (elegibilidade is null)
        {
            return new ResultadoPolicy
            {
                Disponivel = false,
                Motivo =
                    "Não foi possível consultar a elegibilidade para recurso JARI."
            };
        }

        if (!elegibilidade.Permitido)
        {
            return new ResultadoPolicy
            {
                Disponivel = false,
                Motivo =
                    elegibilidade.Motivo ??
                    "O auto não está elegível para recurso JARI."
            };
        }

        return new ResultadoPolicy
        {
            Disponivel = true,

            Observacao =
                contexto.Auto.DataVencimentoNip.HasValue
                    ? $"Prazo para recurso JARI: {contexto.Auto.DataVencimentoNip.Value:dd/MM/yyyy}."
                    : null
        };
    }
}
