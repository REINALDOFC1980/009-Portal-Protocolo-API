using Dapper;
using Portal.Protocolos.API.Models.Atendimento.Ait;
using Portal.Protocolos.API.Models.Atendimento.Condutor;
using Portal.Protocolos.API.Models.Atendimento.Processo;
using Portal.Protocolos.Infrastructure.Database.Connection;
using System.Data;

namespace Portal.Protocolos.API.Repositories;

public sealed class ProcessoCondutorRepository
{
    private readonly DbSession _dbSession;

    public ProcessoCondutorRepository(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<long> InserirAsync(
        ProcessoCondutor processoCondutor)
    {
        using var connection =
            _dbSession.CreateConnection();

        return await InserirAsync(
            processoCondutor,
            connection,
            null);
    }

    public async Task<long> InserirAsync(
        ProcessoCondutor processoCondutor,
        IDbConnection connection,
        IDbTransaction? transaction)
    {
        const string sql = @"
            INSERT INTO ProcessoCondutor
            (
                ProcessoId,
                DocumentoCondutor,
                NomeCondutor,
                RegistroCnh,
                UfCnh,
                ModeloCnh,
                UsuarioCondutorId,
                Aceito,
                DataAceite,
                DataRecusa,
                MotivoRecusa
            )
            VALUES
            (
                @ProcessoId,
                @DocumentoCondutor,
                @NomeCondutor,
                @RegistroCnh,
                @UfCnh,
                @ModeloCnh,
                @UsuarioCondutorId,
                @Aceito,
                @DataAceite,
                @DataRecusa,
                @MotivoRecusa
            );

            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        return await connection.ExecuteScalarAsync<long>(
            sql,
            processoCondutor,
            transaction);
    }

    public async Task<List<ProcessoCondutor>>
        ListarPendentesPorDocumentoAsync(
            string documento)
    {
        const string sql = @"
            SELECT
                ProcessoCondutorId,
                ProcessoId,
                DocumentoCondutor,
                NomeCondutor,
                RegistroCnh,
                UfCnh,
                ModeloCnh,
                UsuarioCondutorId,
                Aceito,
                DataIndicacao,
                DataAceite,
                DataRecusa,
                MotivoRecusa
            FROM ProcessoCondutor
            WHERE DocumentoCondutor = @Documento
              AND Aceito IS NULL
            ORDER BY DataIndicacao DESC;";

        using var connection =
            _dbSession.CreateConnection();

        var processos =
            await connection.QueryAsync<ProcessoCondutor>(
                sql,
                new
                {
                    Documento = documento
                });

        return processos.ToList();
    }

    public async Task<List<CondutorPendenteResponse>>
        ListarProcessosPendentesAsync(
            string documento)
    {
        const string sql = @"
            SELECT
                PC.ProcessoCondutorId,
                P.ProcessoId,
                P.NumeroProcesso,
                P.NumeroAIT,
                P.Placa,
                P.CodigoInfracao,
                P.DescricaoInfracao,
                P.DataInfracao,
                P.LocalInfracao,
                P.MarcaModelo,
                P.Proprietario,
                P.DocumentoProprietario,
                P.PrazoApresentacaoCondutor,
                PC.DataIndicacao
            FROM ProcessoCondutor PC
            INNER JOIN Processo P
                ON P.ProcessoId = PC.ProcessoId
            INNER JOIN SituacaoProcesso SP
                ON SP.SituacaoProcessoId = P.SituacaoProcessoId
            WHERE PC.DocumentoCondutor = @Documento
              AND PC.Aceito IS NULL
              AND SP.Codigo = 'AGUARDANDO_ACEITE_CONDUTOR'
            ORDER BY PC.DataIndicacao DESC;";

        using var connection =
            _dbSession.CreateConnection();

        var resultado =
            await connection.QueryAsync<CondutorPendenteResponse>(
                sql,
                new
                {
                    Documento = documento
                });

        return resultado.ToList();
    }

    public async Task<bool> PossuiIndicacaoPendenteAsync(
        long processoId,
        string documentoCondutor)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM ProcessoCondutor
            WHERE ProcessoId = @ProcessoId
              AND DocumentoCondutor = @DocumentoCondutor
              AND Aceito IS NULL;";

        using var connection =
            _dbSession.CreateConnection();

        var quantidade =
            await connection.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    ProcessoId = processoId,
                    DocumentoCondutor = documentoCondutor
                });

        return quantidade > 0;
    }

    public async Task<ProcessoCondutor?> ObterPorIdAsync(
        long processoCondutorId)
    {
        const string sql = @"
            SELECT
                ProcessoCondutorId,
                ProcessoId,
                DocumentoCondutor,
                NomeCondutor,
                RegistroCnh,
                UfCnh,
                ModeloCnh,
                UsuarioCondutorId,
                Aceito,
                DataIndicacao,
                DataAceite,
                DataRecusa,
                MotivoRecusa
            FROM ProcessoCondutor
            WHERE ProcessoCondutorId = @ProcessoCondutorId;";

        using var connection =
            _dbSession.CreateConnection();

        return await connection
            .QueryFirstOrDefaultAsync<ProcessoCondutor>(
                sql,
                new
                {
                    ProcessoCondutorId =
                        processoCondutorId
                });
    }

    public async Task AceitarAsync(
        long processoCondutorId,
        int usuarioCondutorId)
    {
        using var connection =
            _dbSession.CreateConnection();

        await AceitarAsync(
            processoCondutorId,
            usuarioCondutorId,
            connection,
            null);
    }

    public async Task AceitarAsync(
        long processoCondutorId,
        int usuarioCondutorId,
        IDbConnection connection,
        IDbTransaction? transaction)
    {
        const string sql = @"
            UPDATE ProcessoCondutor
            SET
                UsuarioCondutorId = @UsuarioCondutorId,
                Aceito = 1,
                DataAceite = GETDATE(),
                DataRecusa = NULL,
                MotivoRecusa = NULL
            WHERE ProcessoCondutorId = @ProcessoCondutorId
              AND Aceito IS NULL;";

        await connection.ExecuteAsync(
            sql,
            new
            {
                ProcessoCondutorId =
                    processoCondutorId,

                UsuarioCondutorId =
                    usuarioCondutorId
            },
            transaction);
    }

    public async Task AceitarComDadosAsync(
        long processoCondutorId,
        int usuarioCondutorId,
        string nomeCondutor,
        string registroCnh,
        string ufCnh,
        byte modeloCnh,
        IDbConnection connection,
        IDbTransaction? transaction)
    {
        const string sql = @"
            UPDATE ProcessoCondutor
            SET
                NomeCondutor = @NomeCondutor,
                RegistroCnh = @RegistroCnh,
                UfCnh = @UfCnh,
                ModeloCnh = @ModeloCnh,
                UsuarioCondutorId = @UsuarioCondutorId,
                Aceito = 1,
                DataAceite = GETDATE(),
                DataRecusa = NULL,
                MotivoRecusa = NULL
            WHERE ProcessoCondutorId = @ProcessoCondutorId
              AND Aceito IS NULL;";

        await connection.ExecuteAsync(
            sql,
            new
            {
                ProcessoCondutorId =
                    processoCondutorId,

                UsuarioCondutorId =
                    usuarioCondutorId,

                NomeCondutor =
                    nomeCondutor,

                RegistroCnh =
                    registroCnh,

                UfCnh =
                    ufCnh,

                ModeloCnh =
                    modeloCnh.ToString()
            },
            transaction);
    }

    public async Task RecusarAsync(
        long processoCondutorId,
        int usuarioCondutorId,
        string? motivo)
    {
        const string sql = @"
            UPDATE ProcessoCondutor
            SET
                UsuarioCondutorId = @UsuarioCondutorId,
                Aceito = 0,
                DataRecusa = GETDATE(),
                DataAceite = NULL,
                MotivoRecusa = @Motivo
            WHERE ProcessoCondutorId = @ProcessoCondutorId
              AND Aceito IS NULL;";

        using var connection =
            _dbSession.CreateConnection();

        await connection.ExecuteAsync(
            sql,
            new
            {
                ProcessoCondutorId =
                    processoCondutorId,

                UsuarioCondutorId =
                    usuarioCondutorId,

                Motivo =
                    motivo
            });
    }

    public async Task<CondutorVinculadoAitResponse?> ObterVinculadoPorProcessoAsync(
        string numeroAit,
        string numeroProcesso)
    {
        const string sql = @"
            SELECT TOP (1)
                PC.ProcessoCondutorId,
                PC.ProcessoId,
                P.NumeroProcesso,
                P.NumeroAIT AS NumeroAit,
                PC.DocumentoCondutor,
                PC.NomeCondutor,
                PC.UsuarioCondutorId,
                PC.Aceito
            FROM ProcessoCondutor PC
            INNER JOIN Processo P
                ON P.ProcessoId = PC.ProcessoId
            WHERE P.NumeroAIT = @NumeroAit
              AND P.NumeroProcesso = @NumeroProcesso
              AND PC.Aceito = 1
            ORDER BY PC.DataAceite DESC, PC.ProcessoCondutorId DESC;";

        using var connection = _dbSession.CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<CondutorVinculadoAitResponse>(
            sql,
            new
            {
                NumeroAit = numeroAit,
                NumeroProcesso = numeroProcesso
            });
    }

    public async Task<List<string>> ListarNumerosAitAceitosPorDocumentoAsync(
        string documento)
    {
        const string sql = @"
            SELECT DISTINCT P.NumeroAIT
            FROM ProcessoCondutor PC
            INNER JOIN Processo P
                ON P.ProcessoId = PC.ProcessoId
            WHERE PC.DocumentoCondutor = @Documento
              AND PC.Aceito = 1
              AND P.NumeroAIT IS NOT NULL
              AND LTRIM(RTRIM(P.NumeroAIT)) <> '';";

        using var connection = _dbSession.CreateConnection();

        var numeros = await connection.QueryAsync<string>(
            sql,
            new { Documento = documento });

        return numeros
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

}
