namespace Portal.Protocolos.API.Models.Atendimento.Movimentacao
{
    public sealed class ProcessoMovimentacao
    {
        public long ProcessoMovimentacaoId { get; set; }

        public long ProcessoId { get; set; }

        public int? UsuarioId { get; set; }

        public int? SituacaoProcessoId { get; set; }

        public string Acao { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        public DateTime DataMovimentacao { get; set; }
    }
}
