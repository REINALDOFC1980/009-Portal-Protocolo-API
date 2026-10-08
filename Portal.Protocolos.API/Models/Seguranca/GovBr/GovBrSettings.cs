namespace Portal.Protocolos.API.Models.Seguranca.GovBr;

public sealed class GovBrSettings
{
    public bool ModoSimulacao { get; set; }

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string FrontEndUrl { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public GovBrSimulacaoSettings Simulacao { get; set; } = new();
}

public sealed class GovBrSimulacaoSettings
{
    public string Cpf { get; set; } = "11144477735";

    public string Nome { get; set; } = "CIDADÃO TESTE GOV.BR";

    public string? NomeSocial { get; set; }

    public string? Email { get; set; } = "cidadao.teste@localhost";

    public bool EmailVerificado { get; set; } = true;

    public string? Telefone { get; set; } = "71999999999";

    public bool TelefoneVerificado { get; set; } = true;
}
