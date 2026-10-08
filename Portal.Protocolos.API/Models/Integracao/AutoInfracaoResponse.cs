namespace Portal.Protocolos.API.Models.Integracao;

public sealed class AutoInfracaoResponse
{
    public string NumeroAuto { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;
    public string CodigoInfracao { get; set; } = string.Empty;
    public string DescricaoInfracao { get; set; } = string.Empty;

    public DateTime? DataInfracao { get; set; }

    public string LocalInfracao { get; set; } = string.Empty;
    public string Situacao { get; set; } = string.Empty;
    public string Referencia { get; set; } = string.Empty;

    public DateTime? PrazoDefesa { get; set; }
    public DateTime? PrazoApresentacaoCondutor { get; set; }

    public DateTime? DataPostagemNai { get; set; }
    public DateTime? DataPublicacaoNai { get; set; }

    public DateTime? DataPostagemNip { get; set; }
    public DateTime? DataPublicacaoNip { get; set; }

    public DateTime? DataVencimentoNip { get; set; }

    public string Renavam { get; set; } = string.Empty;
    public string MarcaModelo { get; set; } = string.Empty;
    public string Proprietario { get; set; } = string.Empty;
    public string DocumentoProprietario { get; set; } = string.Empty;

    public decimal Valor { get; set; }
}
