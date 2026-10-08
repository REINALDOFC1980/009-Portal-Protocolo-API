namespace Portal.Protocolos.API.Models.Atendimento.Defesa;

public sealed class DefesaResponse
{
    public long ProcessoId { get; set; }

    public string Protocolo { get; set; } =
        string.Empty;

    public string NumeroAit { get; set; } =
        string.Empty;

    public DateOnly? DataAbertura { get; set; }

    public DateTime? DataCadastro { get; set; }

    public string? UsuarioCadastro { get; set; }

    public string? ResultadoCodigo { get; set; }

    public string? ResultadoDescricao { get; set; }

    public DateTime? DataResultado { get; set; }

    public DateTime? DataBaixa { get; set; }

    public string? UsuarioBaixa { get; set; }

    public DateTime? DataPublicacao { get; set; }

    public bool EfeitoSuspensivoAplicado { get; set; }

    public DateTime? CancelavelAte { get; set; }
}
