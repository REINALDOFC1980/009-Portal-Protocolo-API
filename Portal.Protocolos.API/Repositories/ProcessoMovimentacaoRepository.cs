using Dapper;
using Portal.Protocolos.API.Models.Atendimento.Movimentacao;
using Portal.Protocolos.Infrastructure.Database.Connection;
using System.Data;

namespace Portal.Protocolos.API.Repositories
{
    public class ProcessoMovimentacaoRepository
    {
        private readonly DbSession _db;

        public ProcessoMovimentacaoRepository(DbSession db)
        {
            _db = db;
        }

        public async Task<long> InserirAsync(
            ProcessoMovimentacao movimentacao)
        {
            using var conn = _db.CreateConnection();

            return await InserirAsync(
                movimentacao,
                conn,
                null);
        }

        public async Task<long> InserirAsync(
            ProcessoMovimentacao movimentacao,
            IDbConnection connection,
            IDbTransaction? transaction)
        {
            const string sql = @"
                INSERT INTO ProcessoMovimentacao
                (
                    ProcessoId,
                    UsuarioId,
                    SituacaoProcessoId,
                    Acao,
                    Descricao,
                    DataMovimentacao
                )
                VALUES
                (
                    @ProcessoId,
                    @UsuarioId,
                    @SituacaoProcessoId,
                    @Acao,
                    @Descricao,
                    SYSDATETIME()
                );

                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
            ";

            return await connection.ExecuteScalarAsync<long>(
                sql,
                movimentacao,
                transaction);
        }

        public async Task<IEnumerable<ProcessoMovimentacao>>
            ListarPorProcessoAsync(long processoId)
        {
            using var conn = _db.CreateConnection();

            const string sql = @"
                SELECT
                    ProcessoMovimentacaoId,
                    ProcessoId,
                    UsuarioId,
                    SituacaoProcessoId,
                    Acao,
                    Descricao,
                    DataMovimentacao
                FROM ProcessoMovimentacao
                WHERE ProcessoId = @ProcessoId
                ORDER BY DataMovimentacao;
            ";

            return await conn.QueryAsync<ProcessoMovimentacao>(
                sql,
                new
                {
                    ProcessoId = processoId
                });
        }
    }
}
