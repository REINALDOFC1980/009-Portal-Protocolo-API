namespace Portal.Protocolos.API.Models.Seguranca.GovBr;

public sealed class GovBrAuthState
{
    public string State { get; set; } = string.Empty;

    public string Nonce { get; set; } = string.Empty;

    public string CodeVerifier { get; set; } = string.Empty;

    public DateTime ExpiraEm { get; set; }
}
