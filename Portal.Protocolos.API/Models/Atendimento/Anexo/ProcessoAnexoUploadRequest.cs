namespace Portal.Protocolos.API.Models.Atendimento.Anexo
{
    public sealed class ProcessoAnexoUploadRequest
    {
        public IFormFile Arquivo { get; set; } = null!;

        public string? TipoDocumento { get; set; }
    }
}
