namespace Portal.Protocolos.API.Models.Seguranca.GovBr;

public sealed class GovBrCadastroState
{
    public string TokenCadastro { get; set; } =
        string.Empty;

    public string Cpf { get; set; } =
        string.Empty;

    public string Nome { get; set; } =
        string.Empty;

    public string? NomeSocial { get; set; }

    public string? Email { get; set; }

    public bool? EmailVerificado { get; set; }

    public string? Telefone { get; set; }

    public bool? TelefoneVerificado { get; set; }

    public DateTime ExpiraEm { get; set; }
}
