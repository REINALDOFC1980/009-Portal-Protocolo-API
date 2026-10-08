namespace Portal.Protocolos.API.Models.Seguranca.GovBr;

public sealed class GovBrCallbackRequest
{
    public string? Code { get; set; }

    public string? State { get; set; }

    public string? Error { get; set; }

    public string? ErrorDescription { get; set; }
}
