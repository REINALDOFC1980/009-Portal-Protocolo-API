using Dapper;
using System.Data;

namespace Portal.Protocolos.API.Repositories;

public sealed class CondutorPresencialRepository
{
    public async Task<long> InserirAceitoAsync(
        long processoId,
        int usuarioCondutorId,
        string documentoCondutor,
        string nomeCondutor,
        string registroCnh,
        string ufCnh,
        string modeloCnh,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        const string sql = """
            INSERT INTO dbo.ProcessoCondutor
            (
                ProcessoId,
                DocumentoCondutor,
                UsuarioCondutorId,
                Aceito,
                DataIndicacao,
                DataAceite,
                NomeCondutor,
                RegistroCnh,
                UfCnh,
                ModeloCnh
            )
            VALUES
            (
                @ProcessoId,
                @DocumentoCondutor,
                @UsuarioCondutorId,
                1,
                SYSUTCDATETIME(),
                SYSUTCDATETIME(),
                @NomeCondutor,
                @RegistroCnh,
                @UfCnh,
                @ModeloCnh
            );

            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """;

        return await connection.ExecuteScalarAsync<long>(
            sql,
            new
            {
                ProcessoId = processoId,
                UsuarioCondutorId = usuarioCondutorId,
                DocumentoCondutor = documentoCondutor,
                NomeCondutor = nomeCondutor,
                RegistroCnh = registroCnh,
                UfCnh = ufCnh,
                ModeloCnh = modeloCnh
            },
            transaction);
    }
}
