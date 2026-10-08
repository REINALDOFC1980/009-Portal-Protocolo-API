using Portal.Protocolos.API.Models.Atendimento.Processo;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Service;
using Portal.Protocolos.Shared.Exceptions;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class ProcessoPresencialDetalheService
{
    private readonly ProcessoPresencialDetalheRepository _repository;
    private readonly ProcessoAnexoService _anexos;
    private readonly ProcessoMovimentacaoService _movimentacoes;

    public ProcessoPresencialDetalheService(
        ProcessoPresencialDetalheRepository repository,
        ProcessoAnexoService anexos,
        ProcessoMovimentacaoService movimentacoes)
    {
        _repository = repository;
        _anexos = anexos;
        _movimentacoes = movimentacoes;
    }

    public async Task<ProcessoPresencialCompletoResponse?> ObterAsync(long processoId)
    {
        if (processoId <= 0)
            throw new BusinessException("Processo inválido.");

        var processo = await _repository.ObterAsync(processoId);
        if (processo is null) return null;

        var anexos = (await _anexos.ListarPorProcessoAsync(processoId))
            .Select(a => new AnexoPresencialResumoResponse
            {
                ProcessoAnexoId = a.ProcessoAnexoId,
                NomeOriginal = a.NomeOriginal,
                TipoDocumento = a.TipoDocumento,
                TamanhoBytes = a.TamanhoBytes,
                DataCadastro = a.DataCadastro
            }).ToList();

        var movimentacoes = (await _movimentacoes.ListarPorProcessoAsync(processoId))
            .Select(m => new MovimentacaoPresencialResumoResponse
            {
                ProcessoMovimentacaoId = m.ProcessoMovimentacaoId,
                Acao = m.Acao,
                Descricao = m.Descricao,
                SituacaoProcessoId = m.SituacaoProcessoId,
                UsuarioId = m.UsuarioId,
                DataMovimentacao = m.DataMovimentacao
            }).ToList();

        return new ProcessoPresencialCompletoResponse
        {
            Processo = processo,
            Anexos = anexos,
            Movimentacoes = movimentacoes
        };
    }
}
