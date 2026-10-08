using Dapper;
using Portal.Protocolos.API.Models.Integracao;
using Portal.Protocolos.Infrastructure.Database.Connection;

namespace Portal.Protocolos.API.Repositories;

public sealed class AutoInfracaoRepository
{
    private readonly DbSession _dbSession;

    public AutoInfracaoRepository(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<IEnumerable<AutoInfracaoResponse>> ListarPorDocumentoAsync(
        string documento)
    {
        const string sql = @"
            SELECT
                NumeroAuto,
                Placa,
                CodigoInfracao,
                DescricaoInfracao,
                DataInfracao,
                LocalInfracao,
                Situacao,
                Referencia,
                PrazoDefesa,
                PrazoApresentacaoCondutor,
                DataPostagemNai,
                DataPublicacaoNai,
                DataPostagemNip,
                DataPublicacaoNip,
                DataVencimentoNip,
                Renavam,
                MarcaModelo,
                Proprietario,
                DocumentoProprietario,
                Valor
            FROM AutoInfracao
            WHERE DocumentoProprietario = @Documento
            ORDER BY DataInfracao DESC;";

        using var connection = _dbSession.CreateConnection();

        return await connection.QueryAsync<AutoInfracaoResponse>(
            sql,
            new
            {
                Documento = documento
            });
    }

    public async Task<AutoInfracaoResponse?> ObterPorNumeroAsync(
        string numeroAuto,
        string documento)
    {
        const string sql = @"
            SELECT
                NumeroAuto,
                Placa,
                CodigoInfracao,
                DescricaoInfracao,
                DataInfracao,
                LocalInfracao,
                Situacao,
                Referencia,
                PrazoDefesa,
                PrazoApresentacaoCondutor,
                DataPostagemNai,
                DataPublicacaoNai,
                DataPostagemNip,
                DataPublicacaoNip,
                DataVencimentoNip,
                Renavam,
                MarcaModelo,
                Proprietario,
                DocumentoProprietario,
                Valor
            FROM AutoInfracao
            WHERE NumeroAuto = @NumeroAuto
              AND DocumentoProprietario = @Documento;";

        using var connection = _dbSession.CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<AutoInfracaoResponse>(
            sql,
            new
            {
                NumeroAuto = numeroAuto,
                Documento = documento
            });
    }
}
