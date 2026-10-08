using Portal.Protocolos.Shared.Constants;

namespace Portal.Protocolos.API.Services.Atendimento.Policies;

public sealed class DefesaPolicy : IServicoPolicy
{
    public short ServicoId =>
        ServicoConstants.Defesa;

    public ResultadoPolicy Avaliar(
        ContextoServico contexto)
    {
        if (contexto.PossuiDefesa)
        {
            return new ResultadoPolicy
            {
                Disponivel = false,
                Motivo =
                    "Já existe processo de Defesa para este auto."
            };
        }

        var elegibilidade =
            contexto.ElegibilidadeSitran?.Ritos.Defesa;

        if (elegibilidade is null)
        {
            return new ResultadoPolicy
            {
                Disponivel = false,
                Motivo =
                    "Não foi possível consultar a elegibilidade da Defesa."
            };
        }

        if (!elegibilidade.Permitido)
        {
            return new ResultadoPolicy
            {
                Disponivel = false,
                Motivo =
                    elegibilidade.Motivo ??
                    "O auto não está elegível para Defesa."
            };
        }

        return new ResultadoPolicy
        {
            Disponivel = true,

            Observacao =
                contexto.Auto.PrazoDefesa.HasValue
                    ? $"Prazo para Defesa: {contexto.Auto.PrazoDefesa.Value:dd/MM/yyyy}."
                    : null
        };
    }
}
