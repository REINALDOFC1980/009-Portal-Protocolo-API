using Portal.Protocolos.API.Models.Integracao;

namespace Portal.Protocolos.API.Models.Atendimento.Ressarcimento;

public sealed class RessarcimentoCreateRequest
{
    public AutoInfracaoResponse Auto { get; set; } = null!;
}
