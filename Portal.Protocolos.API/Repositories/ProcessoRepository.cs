using Dapper;
using Portal.Protocolos.API.Models.Atendimento.Processo;
using Portal.Protocolos.Infrastructure.Database.Connection;
using System.Data;

namespace Portal.Protocolos.API.Repositories;

public sealed class ProcessoRepository
{
    private readonly DbSession _dbSession;

    public ProcessoRepository(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<List<ProcessoPortalResponse>> ListarPorUsuarioAsync(
    int usuarioId,
    string documento)
    {
        const string sql = @"
        SELECT
            P.ProcessoId,
            P.NumeroProcesso AS Protocolo,
            P.NumeroAIT AS NumeroAuto,
            S.Nome AS Servico,
            LOWER(S.Codigo) AS ServicoCodigo,
            P.DataAbertura AS Data,

            CASE
                WHEN P.UsuarioId IS NULL
                     AND PA.ProcessoId IS NOT NULL
                    THEN 'PRESENCIAL'
                ELSE 'WEB'
            END AS Origem,

            CASE P.SituacaoProcessoId
                WHEN 1 THEN 'Aguardando aceite do condutor'
                WHEN 2 THEN 'Recusado pelo condutor'
                WHEN 3 THEN 'Aguardando triagem'
                WHEN 4 THEN 'Pendente de esclarecimento'
                WHEN 5 THEN 'Rejeitado na triagem'
                WHEN 6 THEN 'Aguardando retorno externo'
                WHEN 7 THEN 'Encaminhado para análise externa'
                WHEN 8 THEN 'Aprovado na análise externa'
                WHEN 9 THEN 'Rejeitado na análise externa'
                WHEN 10 THEN 'Prazo de aceite expirado'
                WHEN 11 THEN 'Em análise'
                WHEN 12 THEN 'Rejeitado pelo SITRAN'
                ELSE 'Situação não identificada'
            END AS Situacao,

            PC.DocumentoCondutor,
            PC.NomeCondutor,
            PC.Aceito AS CondutorAceito,
            PC.DataIndicacao AS DataIndicacaoCondutor,
            PC.DataAceite AS DataAceiteCondutor,
            PC.DataRecusa AS DataRecusaCondutor,
            PC.MotivoRecusa AS MotivoRecusaCondutor

        FROM Processo P

        INNER JOIN Servico S
            ON S.ServicoId = P.ServicoId

        LEFT JOIN ProcessoCondutor PC
            ON PC.ProcessoId = P.ProcessoId

        LEFT JOIN ProcessoAtendimento PA
            ON PA.ProcessoId = P.ProcessoId

        WHERE
            (
                P.UsuarioId = @UsuarioId
            )
            OR
            (
                P.UsuarioId IS NULL
                AND PA.ProcessoId IS NOT NULL
                AND
                REPLACE(
                    REPLACE(
                        REPLACE(
                            REPLACE(
                                LTRIM(RTRIM(PA.DocumentoSolicitante)),
                                '.', ''
                            ),
                            '-', ''
                        ),
                        '/', ''
                    ),
                    ' ', ''
                ) = @Documento
            )

        ORDER BY
            P.DataAbertura DESC,
            P.ProcessoId DESC;";

        var documentoNormalizado =
            new string(
                (documento ?? string.Empty)
                    .Where(char.IsDigit)
                    .ToArray());

        using var connection =
            _dbSession.CreateConnection();

        var processos =
            await connection.QueryAsync<ProcessoPortalResponse>(
                sql,
                new
                {
                    UsuarioId = usuarioId,
                    Documento = documentoNormalizado
                });

        return processos.ToList();
    }
    public async Task<List<ProcessoBloqueanteResponse>> ListarUltimosProcessosPorAitAsync(
        string numeroAIT)
    {
        const string sql = @"
            WITH UltimosProcessos AS
            (
                SELECT
                    P.ProcessoId,
                    P.ServicoId,
                    S.Codigo AS CodigoServico,
                    P.NumeroProcesso,
                    P.SituacaoProcessoId,
                    SP.Codigo AS CodigoSituacao,
                    SP.Nome AS Situacao,
                    SP.BloqueiaNovaAbertura,
                    P.DataAbertura,
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY P.ServicoId
                        ORDER BY
                            P.DataAbertura DESC,
                            P.ProcessoId DESC
                    ) AS Ordem
                FROM Processo P
                INNER JOIN Servico S
                    ON S.ServicoId = P.ServicoId
                INNER JOIN SituacaoProcesso SP
                    ON SP.SituacaoProcessoId = P.SituacaoProcessoId
                WHERE UPPER(LTRIM(RTRIM(P.NumeroAIT))) =
                      UPPER(LTRIM(RTRIM(@NumeroAIT)))
            )
            SELECT
                ProcessoId,
                ServicoId,
                CodigoServico,
                NumeroProcesso,
                SituacaoProcessoId,
                CodigoSituacao,
                Situacao,
                BloqueiaNovaAbertura,
                DataAbertura
            FROM UltimosProcessos
            WHERE Ordem = 1;";

        using var connection =
            _dbSession.CreateConnection();

        var processos =
            await connection.QueryAsync<ProcessoBloqueanteResponse>(
                sql,
                new
                {
                    NumeroAIT = numeroAIT
                });

        return processos.ToList();
    }

    public async Task<ProcessoBloqueanteResponse?> ObterProcessoBloqueanteAsync(
        string numeroAIT,
        short servicoId)
    {
        const string sql = @"
            SELECT TOP 1
                P.ProcessoId,
                P.NumeroProcesso,
                P.SituacaoProcessoId,
                SP.Codigo AS CodigoSituacao,
                SP.Nome AS Situacao,
                P.DataAbertura
            FROM Processo P
            INNER JOIN SituacaoProcesso SP
                ON SP.SituacaoProcessoId = P.SituacaoProcessoId
            WHERE P.NumeroAIT = @NumeroAIT
              AND P.ServicoId = @ServicoId
              AND SP.BloqueiaNovaAbertura = 1
            ORDER BY
                P.DataAbertura DESC,
                P.ProcessoId DESC;";

        using var connection =
            _dbSession.CreateConnection();

        return await connection
            .QueryFirstOrDefaultAsync<ProcessoBloqueanteResponse>(
                sql,
                new
                {
                    NumeroAIT = numeroAIT,
                    ServicoId = servicoId
                });
    }

    public async Task<bool> ExisteProcessoBloqueanteAsync(
        string numeroAIT,
        short servicoId)
    {
        using var connection =
            _dbSession.CreateConnection();

        return await ExisteProcessoBloqueanteAsync(
            numeroAIT,
            servicoId,
            connection,
            null);
    }

    public async Task<bool> ExisteProcessoBloqueanteAsync(
        string numeroAIT,
        short servicoId,
        IDbConnection connection,
        IDbTransaction? transaction)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM Processo P
            INNER JOIN SituacaoProcesso SP
                ON SP.SituacaoProcessoId = P.SituacaoProcessoId
            WHERE P.NumeroAIT = @NumeroAIT
              AND P.ServicoId = @ServicoId
              AND SP.BloqueiaNovaAbertura = 1;";

        var quantidade =
            await connection.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    NumeroAIT = numeroAIT,
                    ServicoId = servicoId
                },
                transaction);

        return quantidade > 0;
    }

    public async Task<long> InserirAsync(
        Processo processo)
    {
        using var connection =
            _dbSession.CreateConnection();

        return await InserirAsync(
            processo,
            connection,
            null);
    }

    public async Task<long> InserirAsync(
        Processo processo,
        IDbConnection connection,
        IDbTransaction? transaction)
    {
        const string sql = @"
            INSERT INTO Processo
            (
                NumeroProcesso,
                UsuarioId,
                ServicoId,
                SituacaoProcessoId,
                NumeroAIT,
                Placa,
                Renavam,
                CodigoInfracao,
                DescricaoInfracao,
                DataInfracao,
                LocalInfracao,
                SituacaoAIT,
                PrazoDefesa,
                PrazoApresentacaoCondutor,
                DataVencimentoNip,
                Proprietario,
                DocumentoProprietario,
                MarcaModelo,
                ValorMulta,
                SnapshotAIT,
                MotivoRejeicao
            )
            VALUES
            (
                @NumeroProcesso,
                @UsuarioId,
                @ServicoId,
                @SituacaoProcessoId,
                @NumeroAIT,
                @Placa,
                @Renavam,
                @CodigoInfracao,
                @DescricaoInfracao,
                @DataInfracao,
                @LocalInfracao,
                @SituacaoAIT,
                @PrazoDefesa,
                @PrazoApresentacaoCondutor,
                @DataVencimentoNip,
                @Proprietario,
                @DocumentoProprietario,
                @MarcaModelo,
                @ValorMulta,
                @SnapshotAIT,
                @MotivoRejeicao
            );

            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        return await connection.ExecuteScalarAsync<long>(
            sql,
            processo,
            transaction);
    }

    public async Task<string> GerarNumeroProcessoAsync()
    {
        using var connection =
            _dbSession.CreateConnection();

        return await GerarNumeroProcessoAsync(
            connection,
            null);
    }

    public async Task<string> GerarNumeroProcessoAsync(
        IDbConnection connection,
        IDbTransaction? transaction)
    {
        const string sql = @"
            DECLARE @NumeroProcesso VARCHAR(20);

            EXEC dbo.Spr_GerarNumeroProcesso
                @NumeroProcesso = @NumeroProcesso OUTPUT;

            SELECT @NumeroProcesso;";

        return await connection.ExecuteScalarAsync<string>(
            sql,
            transaction: transaction);
    }

    public async Task AtualizarSituacaoAsync(
        long processoId,
        int situacaoProcessoId)
    {
        using var connection =
            _dbSession.CreateConnection();

        await AtualizarSituacaoAsync(
            processoId,
            situacaoProcessoId,
            connection,
            null);
    }

    public async Task AtualizarSituacaoAsync(
        long processoId,
        int situacaoProcessoId,
        IDbConnection connection,
        IDbTransaction? transaction)
    {
        const string sql = @"
            UPDATE Processo
            SET
                SituacaoProcessoId = @SituacaoProcessoId,
                DataAtualizacao = GETDATE()
            WHERE ProcessoId = @ProcessoId;";

        await connection.ExecuteAsync(
            sql,
            new
            {
                ProcessoId = processoId,
                SituacaoProcessoId = situacaoProcessoId
            },
            transaction);
    }

    public async Task AtualizarSituacaoEMotivoRejeicaoAsync(
        long processoId,
        int situacaoProcessoId,
        string motivoRejeicao,
        IDbConnection connection,
        IDbTransaction? transaction)
    {
        const string sql = @"
            UPDATE Processo
            SET
                SituacaoProcessoId = @SituacaoProcessoId,
                MotivoRejeicao = @MotivoRejeicao,
                DataAtualizacao = GETDATE()
            WHERE ProcessoId = @ProcessoId;";

        await connection.ExecuteAsync(
            sql,
            new
            {
                ProcessoId = processoId,
                SituacaoProcessoId = situacaoProcessoId,
                MotivoRejeicao = motivoRejeicao
            },
            transaction);
    }

    public async Task<Processo?> ObterPorIdAsync(
        long processoId)
    {
        const string sql = @"
            SELECT
                ProcessoId,
                NumeroProcesso,
                UsuarioId,
                ServicoId,
                SituacaoProcessoId,
                NumeroAIT,
                Placa,
                Renavam,
                CodigoInfracao,
                DescricaoInfracao,
                DataInfracao,
                LocalInfracao,
                SituacaoAIT,
                PrazoDefesa,
                PrazoApresentacaoCondutor,
                DataVencimentoNip,
                Proprietario,
                DocumentoProprietario,
                MarcaModelo,
                ValorMulta,
                SnapshotAIT,
                MotivoRejeicao,
                DataAbertura,
                DataAtualizacao
            FROM Processo
            WHERE ProcessoId = @ProcessoId;";

        using var connection =
            _dbSession.CreateConnection();

        return await connection
            .QueryFirstOrDefaultAsync<Processo>(
                sql,
                new
                {
                    ProcessoId = processoId
                });
    }

    public async Task<bool> PertenceAoUsuarioAsync(
        long processoId,
        int usuarioId)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM Processo
            WHERE ProcessoId = @ProcessoId
              AND UsuarioId = @UsuarioId;";

        using var connection =
            _dbSession.CreateConnection();

        var quantidade =
            await connection.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    ProcessoId = processoId,
                    UsuarioId = usuarioId
                });

        return quantidade > 0;
    }

    public async Task<JulgamentoJariContexto?> ObterJulgamentoJariAsync(
        string numeroAIT)
    {
        const string sql = @"
            SELECT TOP 1
                P.ProcessoId,
                PJ.Resultado,
                PJ.DataJulgamento,
                PJ.DataNotificacao,
                PJ.PrazoRecurso
            FROM Processo P
            INNER JOIN Servico S
                ON S.ServicoId = P.ServicoId
            LEFT JOIN ProcessoJulgamento PJ
                ON PJ.ProcessoId = P.ProcessoId
            WHERE P.NumeroAIT = @NumeroAIT
              AND LOWER(S.Codigo) = 'jari'
            ORDER BY P.DataAbertura DESC;";

        using var connection =
            _dbSession.CreateConnection();

        return await connection
            .QueryFirstOrDefaultAsync<JulgamentoJariContexto>(
                sql,
                new
                {
                    NumeroAIT = numeroAIT
                });
    }

    public async Task<bool> UsuarioPodeVisualizarProcessoAsync(
    long processoId,
    int usuarioId,
    string? documento)
    {
        var documentoNormalizado =
            SomenteNumeros(
                documento);

        const string sql = @"
        SELECT
            CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM Processo P

                    LEFT JOIN ProcessoAtendimento PA
                        ON PA.ProcessoId = P.ProcessoId

                    WHERE
                        P.ProcessoId = @ProcessoId
                        AND
                        (
                            P.UsuarioId = @UsuarioId

                            OR

                            (
                                P.UsuarioId IS NULL
                                AND PA.ProcessoId IS NOT NULL
                                AND
                                REPLACE(
                                    REPLACE(
                                        REPLACE(
                                            REPLACE(
                                                REPLACE(
                                                    LTRIM(
                                                        RTRIM(
                                                            ISNULL(
                                                                PA.DocumentoSolicitante,
                                                                ''
                                                            )
                                                        )
                                                    ),
                                                    '.',
                                                    ''
                                                ),
                                                '-',
                                                ''
                                            ),
                                            '/',
                                            ''
                                        ),
                                        ' ',
                                        ''
                                    ),
                                    CHAR(160),
                                    ''
                                ) = @Documento
                            )
                        )
                )
                THEN 1
                ELSE 0
            END;";

        using var connection =
            _dbSession.CreateConnection();

        return await connection
            .ExecuteScalarAsync<bool>(
                sql,
                new
                {
                    ProcessoId = processoId,
                    UsuarioId = usuarioId,
                    Documento = documentoNormalizado
                });
    }

    private static string SomenteNumeros(
        string? valor)
    {
        if (string.IsNullOrWhiteSpace(
                valor))
        {
            return string.Empty;
        }

        return new string(
            valor
                .Where(char.IsDigit)
                .ToArray());
    }
}
