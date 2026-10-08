using Dapper;
using Portal.Protocolos.API.Models.Cadastro.Servico.Entity;
using Portal.Protocolos.Infrastructure.Database.Connection;

namespace Portal.Protocolos.API.Repositories;

public sealed class ServicoRepository
{
    private readonly DbSession _dbSession;

    public ServicoRepository(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<List<Servico>> ListarAtivosAsync()
    {
        const string sql = @"
                SELECT
                    ServicoId,
                    Codigo,
                    Nome,
                    Descricao,
                    ExigeCondutor,
                    Ativo,
                    Ordem,
                    DataCadastro
                FROM Servico
                WHERE Ativo = 1
                ORDER BY Ordem;";

        using var connection = _dbSession.CreateConnection();

        var servicos = await connection.QueryAsync<Servico>(sql);

        return servicos.ToList();
    }
}
