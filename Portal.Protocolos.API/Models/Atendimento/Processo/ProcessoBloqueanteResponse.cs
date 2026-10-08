namespace Portal.Protocolos.API.Models.Atendimento.Processo;

public sealed class ProcessoBloqueanteResponse
{
    public long ProcessoId { get; set; }

    public short ServicoId { get; set; }

    public string CodigoServico { get; set; } = string.Empty;

    public string NumeroProcesso { get; set; } = string.Empty;

    public int SituacaoProcessoId { get; set; }

    public string CodigoSituacao { get; set; } = string.Empty;

    public string Situacao { get; set; } = string.Empty;

    public bool BloqueiaNovaAbertura { get; set; }

    public DateTime DataAbertura { get; set; }
}
