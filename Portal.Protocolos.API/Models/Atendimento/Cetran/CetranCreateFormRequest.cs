using Microsoft.AspNetCore.Http;

namespace Portal.Protocolos.API.Models.Atendimento.Cetran;

public sealed class CetranCreateFormRequest
{
    public string NumeroAuto { get; set; } = string.Empty;

    public List<IFormFile> Arquivos { get; set; } = new();

    public List<string> TiposDocumento { get; set; } = new();
}
