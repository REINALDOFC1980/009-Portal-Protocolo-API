using Portal.Protocolos.Shared.Constants;

namespace Portal.Protocolos.API.Services.Atendimento.Policies;

public sealed class CetranPolicy : IServicoPolicy
{
    public short ServicoId =>
        ServicoConstants.Cetran;

    public ResultadoPolicy Avaliar(
        ContextoServico contexto)
    {
        if (contexto.PossuiProcessoCetran)
        {
            return new ResultadoPolicy
            {
                Disponivel = false,
                Motivo =
                    "Já existe recurso CETRAN protocolado para este auto."
            };
        }

        var elegibilidade =
            contexto.ElegibilidadeSitran?.Ritos.Cetran;

        if (elegibilidade is null)
        {
            return new ResultadoPolicy
            {
                Disponivel = false,
                Motivo =
                    "Não foi possível consultar a elegibilidade para recurso CETRAN."
            };
        }

        if (!elegibilidade.Permitido)
        {
            return new ResultadoPolicy
            {
                Disponivel = false,
                Motivo =
                    elegibilidade.Motivo ??
                    "O auto não está elegível para recurso CETRAN."
            };
        }

        return new ResultadoPolicy
        {
            Disponivel = true,

            Observacao =
                contexto.PrazoCetran.HasValue
                    ? $"Prazo para recurso CETRAN: {contexto.PrazoCetran.Value:dd/MM/yyyy}."
                    : null
        };
    }
}
