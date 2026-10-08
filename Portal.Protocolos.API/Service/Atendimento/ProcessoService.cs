using Portal.Protocolos.API.Models.Atendimento.Processo;
using Portal.Protocolos.API.Models.Integracao;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Service;
using Portal.Protocolos.Shared.Exceptions;
using System.Data;
using System.Text.Json;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class ProcessoService
{
    private readonly ProcessoRepository _processoRepository;
    private readonly ProcessoMovimentacaoService _movimentacaoService;

    public ProcessoService(
        ProcessoRepository processoRepository,
        ProcessoMovimentacaoService movimentacaoService)
    {
        _processoRepository = processoRepository;
        _movimentacaoService = movimentacaoService;
    }

    public async Task<Processo> CriarAsync(
        int usuarioId,
        short servicoId,
        int situacaoProcessoId,
        AutoInfracaoResponse auto)
    {
        var existeProcesso =
            await _processoRepository
                .ExisteProcessoBloqueanteAsync(
                    auto.NumeroAuto,
                    servicoId);

        if (existeProcesso)
        {
            throw new BusinessException(
                "Já existe um processo ativo para este AIT e serviço.");
        }

        var numeroProcesso =
            await _processoRepository
                .GerarNumeroProcessoAsync();

        var processo =
            MontarProcesso(
                usuarioId,
                servicoId,
                situacaoProcessoId,
                auto,
                numeroProcesso);

        processo.ProcessoId =
            await _processoRepository
                .InserirAsync(
                    processo);

        await _movimentacaoService
            .RegistrarAsync(
                processo.ProcessoId,
                usuarioId,
                situacaoProcessoId,
                "PROCESSO_ABERTO",
                $"Processo {numeroProcesso} aberto pelo cidadão.");

        return processo;
    }

    public async Task<Processo> CriarAsync(
        int usuarioId,
        short servicoId,
        int situacaoProcessoId,
        AutoInfracaoResponse auto,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var existeProcesso =
            await _processoRepository
                .ExisteProcessoBloqueanteAsync(
                    auto.NumeroAuto,
                    servicoId,
                    connection,
                    transaction);

        if (existeProcesso)
        {
            throw new BusinessException(
                "Já existe um processo ativo para este AIT e serviço.");
        }

        var numeroProcesso =
            await _processoRepository
                .GerarNumeroProcessoAsync(
                    connection,
                    transaction);

        var processo =
            MontarProcesso(
                usuarioId,
                servicoId,
                situacaoProcessoId,
                auto,
                numeroProcesso);

        processo.ProcessoId =
            await _processoRepository
                .InserirAsync(
                    processo,
                    connection,
                    transaction);

        await _movimentacaoService
            .RegistrarAsync(
                processo.ProcessoId,
                usuarioId,
                situacaoProcessoId,
                "PROCESSO_ABERTO",
                $"Processo {numeroProcesso} aberto pelo cidadão.",
                connection,
                transaction);

        return processo;
    }

    private static Processo MontarProcesso(
        int usuarioId,
        short servicoId,
        int situacaoProcessoId,
        AutoInfracaoResponse auto,
        string numeroProcesso)
    {
        return new Processo
        {
            NumeroProcesso =
                numeroProcesso,

            UsuarioId =
                usuarioId,

            ServicoId =
                servicoId,

            SituacaoProcessoId =
                situacaoProcessoId,

            NumeroAIT =
                auto.NumeroAuto,

            Placa =
                auto.Placa,

            Renavam =
                auto.Renavam,

            CodigoInfracao =
                auto.CodigoInfracao,

            DescricaoInfracao =
                auto.DescricaoInfracao,

            DataInfracao =
                auto.DataInfracao,

            LocalInfracao =
                auto.LocalInfracao,

            SituacaoAIT =
                auto.Situacao,

            PrazoDefesa =
                auto.PrazoDefesa,

            PrazoApresentacaoCondutor =
                auto.PrazoApresentacaoCondutor,

            DataVencimentoNip =
                auto.DataVencimentoNip,

            Proprietario =
                auto.Proprietario,

            DocumentoProprietario =
                auto.DocumentoProprietario,

            MarcaModelo =
                auto.MarcaModelo,

            ValorMulta =
                auto.Valor,

            SnapshotAIT =
                JsonSerializer.Serialize(auto)
        };
    }
}
