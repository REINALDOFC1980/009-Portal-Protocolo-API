namespace Portal.Protocolos.API.Models.Atendimento.Condutor;

public sealed class CondutorPendenteResponse
{
    public long ProcessoCondutorId { get; set; }
    public long ProcessoId { get; set; }
    public string NumeroProcesso { get; set; } = string.Empty;
    public string NumeroAIT { get; set; } = string.Empty;
    public string? Placa { get; set; }
    public string? CodigoInfracao { get; set; }
    public string? DescricaoInfracao { get; set; }
    public DateTime? DataInfracao { get; set; }
    public string? LocalInfracao { get; set; }
    public string? MarcaModelo { get; set; }
    public string? Proprietario { get; set; }
    public string? DocumentoProprietario { get; set; }
    public DateTime? PrazoApresentacaoCondutor { get; set; }
    public DateTime DataIndicacao { get; set; }
}
