namespace Portal.Protocolos.API.Models.Atendimento.Ait;

public sealed class ResponsavelAitResponse
{
    public string Tipo { get; set; } = "PROPRIETARIO";
    public string Nome { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;

    public bool CondutorReconhecido { get; set; }
    public bool ApresentacaoCondutorPendente { get; set; }
    public bool ApresentacaoCondutorIndeferida { get; set; }

    public bool DefesaBloqueada { get; set; }
    public string? MotivoBloqueioDefesa { get; set; }

    public long? ProcessoCondutorId { get; set; }
    public long? ProcessoId { get; set; }
    public string? NumeroProcessoCondutor { get; set; }

    public string? ResultadoCodigo { get; set; }
    public string? ResultadoDescricao { get; set; }
    public string? RetornoDetran { get; set; }
}

public sealed class CondutorVinculadoAitResponse
{
    public long ProcessoCondutorId { get; set; }
    public long ProcessoId { get; set; }
    public string NumeroProcesso { get; set; } = string.Empty;
    public string NumeroAit { get; set; } = string.Empty;
    public string DocumentoCondutor { get; set; } = string.Empty;
    public string? NomeCondutor { get; set; }
    public int? UsuarioCondutorId { get; set; }
    public bool? Aceito { get; set; }
}
