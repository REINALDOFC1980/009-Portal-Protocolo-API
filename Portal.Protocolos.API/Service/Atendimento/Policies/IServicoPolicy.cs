namespace Portal.Protocolos.API.Services.Atendimento.Policies;

public interface IServicoPolicy
{
    short ServicoId { get; }

    ResultadoPolicy Avaliar(ContextoServico contexto);
}
