using Portal.Protocolos.API.Models.Cadastros.Servico.Response;
using Portal.Protocolos.API.Models.Integracao;

namespace Portal.Protocolos.API.Models.Atendimento.Ait;

public sealed class AitAtendimentoDisponibilidadeResponse
{
    public AutoInfracaoResponse AutoInfracao { get; set; } = new();

    public ResponsavelAitResponse ResponsavelAtual { get; set; } = new();

    public List<ServicoDisponivelResponse> Servicos { get; set; } = [];
}
