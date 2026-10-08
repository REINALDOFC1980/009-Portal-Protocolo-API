using Microsoft.AspNetCore.Http;

namespace Portal.Protocolos.API.Models.Atendimento.Jari;

public sealed class JariCreateFormRequest
{
    public string NumeroAuto { get; set; } = string.Empty;

    public List<IFormFile> Arquivos { get; set; } = new();

    public List<string> TiposDocumento { get; set; } = new();
}
