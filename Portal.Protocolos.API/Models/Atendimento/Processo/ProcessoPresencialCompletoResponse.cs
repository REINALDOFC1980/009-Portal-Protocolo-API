namespace Portal.Protocolos.API.Models.Atendimento.Processo;

public sealed class ProcessoPresencialCompletoResponse
{
    public ProcessoPresencialDetalheResponse Processo { get; set; } = new();
    public IReadOnlyList<AnexoPresencialResumoResponse> Anexos { get; set; } = [];
    public IReadOnlyList<MovimentacaoPresencialResumoResponse> Movimentacoes { get; set; } = [];
}

public sealed class AnexoPresencialResumoResponse
{
    public long ProcessoAnexoId { get; set; }
    public string NomeOriginal { get; set; } = string.Empty;
    public string? TipoDocumento { get; set; }
    public long TamanhoBytes { get; set; }
    public DateTime DataCadastro { get; set; }
}

public sealed class MovimentacaoPresencialResumoResponse
{
    public long ProcessoMovimentacaoId { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int? SituacaoProcessoId { get; set; }
    public int? UsuarioId { get; set; }
    public DateTime DataMovimentacao { get; set; }
}
