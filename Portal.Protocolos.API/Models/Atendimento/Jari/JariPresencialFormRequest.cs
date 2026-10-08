using Microsoft.AspNetCore.Http;

namespace Portal.Protocolos.API.Models.Atendimento.Jari;

public sealed class JariPresencialFormRequest
{
    // SETAP: obrigatório, vincula o AIT e o CPF ao agendamento.
    // CORREIOS: não informar; preencher NumeroAuto e DocumentoSolicitante.
    public string? AgendamentoId { get; set; }
    public string Origem { get; set; } = string.Empty;
    public DateOnly? DataPostagem { get; set; }
    public string? NumeroAuto { get; set; }
    public string? DocumentoSolicitante { get; set; }
    public string? TipoSolicitante { get; set; }
    public string NomeSolicitante { get; set; } = string.Empty;
    public bool Estrangeiro { get; set; }

    // Quando houver documento obrigatório não conferido/sem imagem,
    // o Front precisa confirmar explicitamente a continuidade.
    public bool PermitirFaltaDocumentacao { get; set; }

    // Códigos dos documentos obrigatórios que o atendente declarou
    // como não conferidos no checklist.
    public List<string> DocumentosNaoConferidos { get; set; } = new();

    public List<IFormFile> Arquivos { get; set; } = new();
    public List<string> TiposDocumento { get; set; } = new();
}
