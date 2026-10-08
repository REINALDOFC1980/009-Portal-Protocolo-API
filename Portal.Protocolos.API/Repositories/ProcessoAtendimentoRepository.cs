using Dapper;
using Portal.Protocolos.API.Models.Integracao;
using System.Data;
using System.Text.Json;

namespace Portal.Protocolos.API.Repositories;

public sealed class ProcessoAtendimentoRepository
{
    public async Task<long> InserirProcessoPresencialAsync(
        string protocolo,
        short servicoId,
        int situacao,
        AutoInfracaoResponse auto,
        DateTime? dataAbertura,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        const string sql = """
            INSERT INTO dbo.Processo
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
                DataAbertura
            )
            VALUES
            (
                @Protocolo,
                NULL,
                @ServicoId,
                @Situacao,
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
                COALESCE(@DataAbertura, SYSDATETIME())
            );

            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """;

        return await connection.ExecuteScalarAsync<long>(
            sql,
            new
            {
                Protocolo = protocolo,
                ServicoId = servicoId,
                Situacao = situacao,
                NumeroAIT = auto.NumeroAuto,
                auto.Placa,
                auto.Renavam,
                auto.CodigoInfracao,
                auto.DescricaoInfracao,
                auto.DataInfracao,
                auto.LocalInfracao,
                SituacaoAIT = auto.Situacao,
                auto.PrazoDefesa,
                auto.PrazoApresentacaoCondutor,
                auto.DataVencimentoNip,
                auto.Proprietario,
                auto.DocumentoProprietario,
                auto.MarcaModelo,
                ValorMulta = auto.Valor,
                SnapshotAIT = JsonSerializer.Serialize(auto),
                DataAbertura = dataAbertura
            },
            transaction);
    }

    public async Task InserirAtendimentoAsync(
        long processoId,
        int atendenteId,
        string? agendamentoId,
        string origem,
        string tipoSolicitante,
        string nome,
        string cpf,
        byte? restricao,
        DateOnly? dataPostagem,
        bool faltaDocumentacao,
        string? documentosFaltantes,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        const string sql = """
            INSERT INTO dbo.ProcessoAtendimento
            (
                ProcessoId,
                AtendenteUsuarioId,
                AgendamentoId,
                Origem,
                TipoSolicitante,
                NomeSolicitante,
                DocumentoSolicitante,
                TipoRestricaoId,
                DataPostagem,
                FaltaDocumentacao,
                DocumentosFaltantes
            )
            VALUES
            (
                @ProcessoId,
                @AtendenteId,
                @AgendamentoId,
                @Origem,
                @TipoSolicitante,
                @Nome,
                @Cpf,
                @Restricao,
                @DataPostagem,
                @FaltaDocumentacao,
                @DocumentosFaltantes
            );
            """;

        await connection.ExecuteAsync(
            sql,
            new
            {
                ProcessoId = processoId,
                AtendenteId = atendenteId,
                AgendamentoId = agendamentoId,
                Origem = origem,
                TipoSolicitante = tipoSolicitante,
                Nome = nome,
                Cpf = cpf,
                Restricao = restricao,

                // Evita enviar System.DateOnly diretamente ao SqlClient/Dapper.
                // A coluna no SQL Server é DATE; DateTime é convertido normalmente.
                DataPostagem = dataPostagem.HasValue
                    ? dataPostagem.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,

                FaltaDocumentacao = faltaDocumentacao,
                DocumentosFaltantes = documentosFaltantes
            },
            transaction);
    }

    public async Task<bool> AgendamentoJaAtendidoAsync(
        string agendamentoId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.ProcessoAtendimento
                WHERE AgendamentoId = @AgendamentoId
            )
            THEN 1 ELSE 0 END;
            """;

        return await connection.ExecuteScalarAsync<bool>(
            sql,
            new
            {
                AgendamentoId = agendamentoId
            },
            transaction);
    }
}
