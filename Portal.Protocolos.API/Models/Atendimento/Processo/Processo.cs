namespace Portal.Protocolos.API.Models.Atendimento.Processo;

public sealed class Processo
{
    public long ProcessoId { get; set; }
    public string NumeroProcesso { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public short ServicoId { get; set; }
    public int SituacaoProcessoId { get; set; }
    public string NumeroAIT { get; set; } = string.Empty;
    public string? Placa { get; set; }
    public string? Renavam { get; set; }
    public string? CodigoInfracao { get; set; }
    public string? DescricaoInfracao { get; set; }
    public DateTime? DataInfracao { get; set; }
    public string? LocalInfracao { get; set; }
    public string? SituacaoAIT { get; set; }
    public DateTime? PrazoDefesa { get; set; }
    public DateTime? PrazoApresentacaoCondutor { get; set; }
    public DateTime? DataVencimentoNip { get; set; }
    public string? Proprietario { get; set; }
    public string? DocumentoProprietario { get; set; }
    public string? MarcaModelo { get; set; }
    public decimal? ValorMulta { get; set; }
    public string? SnapshotAIT { get; set; }
    public string? MotivoRejeicao { get; set; }
    public DateTime DataAbertura { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}
