using Portal.Protocolos.API.Models.Atendimento.Movimentacao;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.Shared.Exceptions;
using System.Data;

namespace Portal.Protocolos.API.Service
{
    public class ProcessoMovimentacaoService
    {
        private readonly ProcessoMovimentacaoRepository _repository;

        public ProcessoMovimentacaoService(
            ProcessoMovimentacaoRepository repository)
        {
            _repository = repository;
        }

        public async Task<long> RegistrarAsync(
            long processoId,
            int? usuarioId,
            int? situacaoProcessoId,
            string acao,
            string? descricao)
        {
            if (processoId <= 0)
            {
                throw new BusinessException(
                    "Processo inválido.");
            }

            if (string.IsNullOrWhiteSpace(acao))
            {
                throw new BusinessException(
                    "A ação da movimentação é obrigatória.");
            }

            var movimentacao =
                new ProcessoMovimentacao
                {
                    ProcessoId = processoId,
                    UsuarioId = usuarioId,
                    SituacaoProcessoId = situacaoProcessoId,
                    Acao = acao.Trim().ToUpperInvariant(),
                    Descricao = descricao
                };

            return await _repository.InserirAsync(
                movimentacao);
        }

        public async Task<long> RegistrarAsync(
            long processoId,
            int? usuarioId,
            int? situacaoProcessoId,
            string acao,
            string? descricao,
            IDbConnection connection,
            IDbTransaction transaction)
        {
            if (processoId <= 0)
            {
                throw new BusinessException(
                    "Processo inválido.");
            }

            if (string.IsNullOrWhiteSpace(acao))
            {
                throw new BusinessException(
                    "A ação da movimentação é obrigatória.");
            }

            var movimentacao =
                new ProcessoMovimentacao
                {
                    ProcessoId = processoId,
                    UsuarioId = usuarioId,
                    SituacaoProcessoId = situacaoProcessoId,
                    Acao = acao.Trim().ToUpperInvariant(),
                    Descricao = descricao
                };

            return await _repository.InserirAsync(
                movimentacao,
                connection,
                transaction);
        }

        public async Task<IEnumerable<ProcessoMovimentacao>>
            ListarPorProcessoAsync(long processoId)
        {
            if (processoId <= 0)
            {
                throw new BusinessException(
                    "Processo inválido.");
            }

            return await _repository
                .ListarPorProcessoAsync(
                    processoId);
        }
    }
}
