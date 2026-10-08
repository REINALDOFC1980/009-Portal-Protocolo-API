using Portal.Protocolos.API.Models.Integracao;

namespace Portal.Protocolos.API.Models.Atendimento.Condutor;

public sealed class CondutorCreateRequest
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

    public AutoInfracaoResponse Auto { get; set; } =
        null!;
}
