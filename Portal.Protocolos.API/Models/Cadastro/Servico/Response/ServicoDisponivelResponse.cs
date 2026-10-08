using Portal.Protocolos.API.Models.Atendimento.Processo;

namespace Portal.Protocolos.API.Models.Cadastros.Servico.Response;

public sealed class ServicoDisponivelResponse
{
    public short ServicoId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public bool ExigeCondutor { get; set; }

    public bool Disponivel { get; set; }

    public string? Motivo { get; set; }

    public string? Observacao { get; set; }

    public List<string> Alertas { get; set; } = [];

    public ProcessoBloqueanteResponse? ProcessoVinculado { get; set; }
}
