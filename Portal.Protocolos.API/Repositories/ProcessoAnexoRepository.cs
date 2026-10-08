using Dapper;
using Portal.Protocolos.API.Models.Atendimento.Anexo;
using Portal.Protocolos.Infrastructure.Database.Connection;
using System.Data;

namespace Portal.Protocolos.API.Repositories
{
    public class ProcessoAnexoRepository
    {
        private readonly DbSession _db;

        public ProcessoAnexoRepository(DbSession db)
        {
            _db = db;
        }

        public async Task<long> InserirAsync(
            ProcessoAnexoModel model)
        {
            using var conn =
                _db.CreateConnection();

            return await InserirAsync(
                model,
                conn,
                null);
        }

        public async Task<long> InserirAsync(
            ProcessoAnexoModel model,
            IDbConnection connection,
            IDbTransaction? transaction)
        {
            const string sql = @"
                INSERT INTO ProcessoAnexo
                (
                    ProcessoId,
                    NomeArquivo,
                    NomeOriginal,
                    Extensao,
                    TamanhoBytes,
                    CaminhoArquivo,
                    TipoDocumento,
                    DataCadastro,
                    Ativo
                )
                VALUES
                (
                    @ProcessoId,
                    @NomeArquivo,
                    @NomeOriginal,
                    @Extensao,
                    @TamanhoBytes,
                    @CaminhoArquivo,
                    @TipoDocumento,
                    SYSDATETIME(),
                    1
                );

                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
            ";

            return await connection.ExecuteScalarAsync<long>(
                sql,
                model,
                transaction);
        }

        public async Task<IEnumerable<ProcessoAnexoModel>>
            ListarPorProcessoAsync(long processoId)
        {
            using var conn =
                _db.CreateConnection();

            const string sql = @"
                SELECT
                    ProcessoAnexoId,
                    ProcessoId,
                    NomeArquivo,
                    NomeOriginal,
                    Extensao,
                    TamanhoBytes,
                    CaminhoArquivo,
                    TipoDocumento,
                    DataCadastro,
                    Ativo
                FROM ProcessoAnexo
                WHERE ProcessoId = @ProcessoId
                  AND Ativo = 1
                ORDER BY DataCadastro DESC;
            ";

            return await conn.QueryAsync<ProcessoAnexoModel>(
                sql,
                new
                {
                    ProcessoId = processoId
                });
        }

        public async Task<ProcessoAnexoModel?> ObterAsync(
            long processoAnexoId)
        {
            using var conn =
                _db.CreateConnection();

            const string sql = @"
                SELECT
                    ProcessoAnexoId,
                    ProcessoId,
                    NomeArquivo,
                    NomeOriginal,
                    Extensao,
                    TamanhoBytes,
                    CaminhoArquivo,
                    TipoDocumento,
                    DataCadastro,
                    Ativo
                FROM ProcessoAnexo
                WHERE ProcessoAnexoId = @ProcessoAnexoId
                  AND Ativo = 1;
            ";

            return await conn
                .QueryFirstOrDefaultAsync<ProcessoAnexoModel>(
                    sql,
                    new
                    {
                        ProcessoAnexoId =
                            processoAnexoId
                    });
        }

        public async Task ExcluirAsync(
            long processoAnexoId)
        {
            using var conn =
                _db.CreateConnection();

            const string sql = @"
                UPDATE ProcessoAnexo
                SET Ativo = 0
                WHERE ProcessoAnexoId = @ProcessoAnexoId;
            ";

            await conn.ExecuteAsync(
                sql,
                new
                {
                    ProcessoAnexoId =
                        processoAnexoId
                });
        }
    }
}
