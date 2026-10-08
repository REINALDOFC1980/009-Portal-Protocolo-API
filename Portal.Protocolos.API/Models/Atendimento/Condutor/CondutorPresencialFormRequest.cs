using Microsoft.AspNetCore.Http;

namespace Portal.Protocolos.API.Models.Atendimento.Condutor;

public sealed class CondutorPresencialFormRequest
{
    // SETAP: obrigatório e vinculado ao agendamento.
    // CORREIOS: não informar AgendamentoId; usar NumeroAuto e DocumentoSolicitante.
    public string? AgendamentoId { get; set; }
    public string Origem { get; set; } = string.Empty;
    public DateOnly? DataPostagem { get; set; }
    public string? NumeroAuto { get; set; }
    public string? DocumentoSolicitante { get; set; }
    public string? TipoSolicitante { get; set; }
    public string NomeSolicitante { get; set; } = string.Empty;
    public bool Estrangeiro { get; set; }

    // No atendimento o CPF apenas seleciona o usuário já cadastrado.
    // Nome/CNH/UF/Modelo são sempre lidos da tabela Usuario no backend.
    public string DocumentoCondutor { get; set; } = string.Empty;

    public bool PermitirFaltaDocumentacao { get; set; }
    public List<string> DocumentosNaoConferidos { get; set; } = new();

    public List<IFormFile> Arquivos { get; set; } = new();
    public List<string> TiposDocumento { get; set; } = new();
}
