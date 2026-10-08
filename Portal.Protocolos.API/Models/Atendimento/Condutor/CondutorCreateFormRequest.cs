using Microsoft.AspNetCore.Http;

namespace Portal.Protocolos.API.Models.Atendimento.Condutor;

public sealed class CondutorCreateFormRequest
{
    public string DocumentoCondutor { get; set; } =
        string.Empty;

    public string NomeCondutor { get; set; } =
        string.Empty;

    public string RegistroCnh { get; set; } =
        string.Empty;

    public string UfCnh { get; set; } =
        string.Empty;

    public string ModeloCnh { get; set; } =
        string.Empty;

    public string NumeroAuto { get; set; } =
        string.Empty;

    public List<IFormFile> Arquivos { get; set; } =
        new();

    public List<string> TiposDocumento { get; set; } =
        new();
}
