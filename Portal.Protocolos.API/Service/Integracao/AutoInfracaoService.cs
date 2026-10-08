using Portal.Protocolos.API.Models.Integracao;

namespace Portal.Protocolos.API.Services.Integracao;

public sealed class AutoInfracaoService
{
    private readonly SitranApiClient _sitranApiClient;

    public AutoInfracaoService(
        SitranApiClient sitranApiClient)
    {
        _sitranApiClient = sitranApiClient;
    }

    public async Task<IEnumerable<AutoInfracaoResponse>> ListarPorDocumentoAsync(
        string documento)
    {
        documento = SomenteNumeros(documento);

        return await _sitranApiClient
            .ListarAitsPorDocumentoAsync(documento);
    }

    public async Task<AutoInfracaoResponse?> ObterPorNumeroAsync(string numeroAuto, string documento)
    {
        documento = SomenteNumeros(documento);

        var auto = await _sitranApiClient.ObterAitAsync(numeroAuto);

        if (auto is null)
        {
            return null;
        }

        var documentoProprietario = SomenteNumeros(documento);

        if (!documentoProprietario.Equals(documento, StringComparison.Ordinal))
        {

            return null;
        }

        return auto;
    }

    private static string SomenteNumeros(string valor)
    {
        return new string(
            valor
                .Where(char.IsDigit)
                .ToArray());
    }
}
