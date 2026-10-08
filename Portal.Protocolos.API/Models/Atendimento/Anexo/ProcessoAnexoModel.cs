namespace Portal.Protocolos.API.Models.Atendimento.Anexo

{
    public sealed class ProcessoAnexoModel
    {
        public long ProcessoAnexoId { get; set; }

        public long ProcessoId { get; set; }

        public string NomeArquivo { get; set; } = string.Empty;

        public string NomeOriginal { get; set; } = string.Empty;

        public string Extensao { get; set; } = string.Empty;

        public long TamanhoBytes { get; set; }

        public string CaminhoArquivo { get; set; } = string.Empty;

        public string? TipoDocumento { get; set; }

        public DateTime DataCadastro { get; set; }

        public bool Ativo { get; set; }
    }
}
