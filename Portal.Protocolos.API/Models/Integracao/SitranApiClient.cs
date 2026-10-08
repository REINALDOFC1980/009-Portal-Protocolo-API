using Portal.Protocolos.API.Models.Integracao;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Portal.Protocolos.API.Services.Integracao;

public sealed class SitranApiClient
{
    private readonly HttpClient _httpClient;

    public SitranApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<AutoInfracaoResponse>>
        ListarAitsPorDocumentoAsync(
            string documento,
            CancellationToken cancellationToken = default)
    {
        using var response =
            await _httpClient.GetAsync(
                $"aits?documento={Uri.EscapeDataString(documento)}",
                cancellationToken);

        await GarantirSucessoAsync(
            response,
            cancellationToken);

        var aits =
            await response.Content
                .ReadFromJsonAsync<List<SitranAitResponse>>(
                    cancellationToken: cancellationToken);

        if (aits is null)
        {
            return Enumerable.Empty<AutoInfracaoResponse>();
        }

        return aits
            .Select(Mapear)
            .ToList();
    }

    public async Task<AutoInfracaoResponse?> ObterAitAsync(string numeroAit, CancellationToken cancellationToken = default)
    {


        using var response =
            await _httpClient.GetAsync($"aits/{Uri.EscapeDataString(numeroAit)}",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        await GarantirSucessoAsync(
            response,
            cancellationToken);

        var ait =
            await response.Content
                .ReadFromJsonAsync<SitranAitResponse>(
                    cancellationToken: cancellationToken);

        if (ait is null)
        {


            return null;
        }

        return Mapear(ait);
    }

    public async Task<SitranElegibilidadeResponse?> ObterElegibilidadeAsync(
        string numeroAit,
        CancellationToken cancellationToken = default)
    {
        using var response =
            await _httpClient.GetAsync(
                $"aits/{Uri.EscapeDataString(numeroAit)}/elegibilidade",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        await GarantirSucessoAsync(
            response,
            cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<SitranElegibilidadeResponse>(
                cancellationToken: cancellationToken);
    }

    public async Task AbrirDefesaAsync(
        string numeroAit,
        string numeroProcesso,
        DateOnly dataAbertura,
        CancellationToken cancellationToken = default)
    {
        var request =
            new SitranAbrirDefesaRequest
            {
                NumeroAit = numeroAit,
                NumeroProcesso = numeroProcesso,
                DataAbertura = dataAbertura
            };

        using var response =
            await _httpClient.PostAsJsonAsync(
                "processos/defesa",
                request,
                cancellationToken);

        await GarantirSucessoAsync(
            response,
            cancellationToken);
    }


    public async Task<SitranDefesaResponse?> ObterDefesaAsync(
    string numeroAit,
    CancellationToken cancellationToken = default)
    {
        using var response =
            await _httpClient.GetAsync(
                $"processos/defesa/{Uri.EscapeDataString(numeroAit)}",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        await GarantirSucessoAsync(
            response,
            cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<SitranDefesaResponse>(
                cancellationToken: cancellationToken);
    }


    public async Task AbrirCondutorAsync(
      string numeroAit,
      string numeroProcesso,
      DateOnly dataApresentacao,
      string nome,
      string registroCnh,
      string ufCnh,
      byte modeloCnh,
      string operador,
      CancellationToken cancellationToken = default)
    {
        var request =
            new SitranAbrirCondutorRequest
            {
                NumeroAit =
                    numeroAit,

                NumeroProcesso =
                    numeroProcesso,

                DataApresentacao =
                    dataApresentacao,

                Condutor =
                    new SitranCondutorRequest
                    {
                        Nome =
                            nome,

                        RegistroCnh =
                            registroCnh,

                        UfCnh =
                            ufCnh,

                        ModeloCnh =
                            modeloCnh
                    },

                /*
                 * O endereço não é utilizado pelo Portal.
                 * O objeto é enviado sem dados para manter
                 * a estrutura do contrato do SITRAN.
                 */
                Endereco =
                    new SitranEnderecoCondutorRequest(),

                Operador =
                    operador
            };

        using var response =
            await _httpClient.PostAsJsonAsync(
                "processos/condutor",
                request,
                cancellationToken);

        await GarantirSucessoAsync(
            response,
            cancellationToken);
    }




    public async Task<SitranCondutorResponse?> ObterCondutorAsync(
     string numeroAit,
     CancellationToken cancellationToken = default)
    {
        using var response =
            await _httpClient.GetAsync(
                $"processos/condutor/{Uri.EscapeDataString(numeroAit)}",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        await GarantirSucessoAsync(
            response,
            cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<SitranCondutorResponse>(
                cancellationToken: cancellationToken);
    }

    public async Task AbrirJariAsync(
    string numeroAit,
    string numeroProcesso,
    DateOnly dataAbertura,
    string operador,
    CancellationToken cancellationToken = default)
    {
        var request =
            new SitranAbrirJariRequest
            {
                NumeroAit =
                    numeroAit,

                NumeroProcesso =
                    numeroProcesso,

                DataAbertura =
                    dataAbertura,

                Operador =
                    operador
            };

        using var response =
            await _httpClient.PostAsJsonAsync(
                "processos/jari",
                request,
                cancellationToken);

        await GarantirSucessoAsync(
            response,
            cancellationToken);
    }


    public async Task<SitranJariResponse?> ObterJariAsync(
        string numeroAit,
        CancellationToken cancellationToken = default)
    {
        using var response =
            await _httpClient.GetAsync(
                $"processos/jari/{Uri.EscapeDataString(numeroAit)}",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        await GarantirSucessoAsync(
            response,
            cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<SitranJariResponse>(
                cancellationToken: cancellationToken);
    }



    public async Task AbrirCetranAsync(
    string numeroAit,
    string numeroProcesso,
    DateOnly dataAbertura,
    string operador,
    CancellationToken cancellationToken = default)
    {
        var request =
            new SitranAbrirCetranRequest
            {
                NumeroAit =
                    numeroAit,

                NumeroProcesso =
                    numeroProcesso,

                DataAbertura =
                    dataAbertura,

                Operador =
                    operador
            };

        using var response =
            await _httpClient.PostAsJsonAsync(
                "processos/cetran",
                request,
                cancellationToken);

        await GarantirSucessoAsync(
            response,
            cancellationToken);
    }


    public async Task<SitranCetranResponse?> ObterCetranAsync(
        string numeroAit,
        CancellationToken cancellationToken = default)
    {
        using var response =
            await _httpClient.GetAsync(
                $"processos/cetran/{Uri.EscapeDataString(numeroAit)}",
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        await GarantirSucessoAsync(
            response,
            cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<SitranCetranResponse>(
                cancellationToken: cancellationToken);
    }

    private static async Task GarantirSucessoAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        SitranProblemDetailsResponse? problema = null;

        try
        {
            problema =
                await response.Content
                    .ReadFromJsonAsync<SitranProblemDetailsResponse>(
                        cancellationToken: cancellationToken);
        }
        catch
        {
            /*
             * Caso o SITRAN retorne uma resposta
             * que não seja JSON válido, ainda
             * preservamos o status HTTP.
             */
        }

        var mensagem =
            problema?.Detail;

        if (string.IsNullOrWhiteSpace(mensagem))
        {
            mensagem =
                problema?.Title;
        }

        if (string.IsNullOrWhiteSpace(mensagem))
        {
            mensagem =
                $"Erro na comunicação com o SITRAN. " +
                $"HTTP {(int)response.StatusCode}.";
        }

        throw new SitranApiException(
            response.StatusCode,
            problema?.Codigo,
            mensagem);
    }

    private static AutoInfracaoResponse Mapear(
        SitranAitResponse ait)
    {
        return new AutoInfracaoResponse
        {
            NumeroAuto =
                ait.NumeroAit ?? string.Empty,

            Placa =
                ait.Placa ?? string.Empty,

            CodigoInfracao =
                ait.Infracao?.Codigo ?? string.Empty,

            DescricaoInfracao =
                ait.Infracao?.Descricao ?? string.Empty,

            DataInfracao =
                ait.Infracao?.Data,

            LocalInfracao =
                ait.Infracao?.Local ?? string.Empty,

            Situacao =
                ait.Situacao?.Descricao ?? string.Empty,

            Referencia =
                ait.Infracao?.Referencia ?? string.Empty,

            PrazoDefesa =
                ait.Prazos?.DefesaNai,

            PrazoApresentacaoCondutor =
                ait.Prazos?.Apresentacao,

            DataVencimentoNip =
                ait.Prazos?.VencimentoNip,

            Renavam =
                ait.Veiculo?.Renavam ?? string.Empty,

            MarcaModelo =
                ait.Veiculo?.Marca ?? string.Empty,

            Proprietario =
                ait.Proprietario?.Nome ?? string.Empty,

            DocumentoProprietario =
                ait.Proprietario?.Documento ?? string.Empty,

            Valor =
                ait.Valores?.Cheio ?? 0m
        };
    }
}


internal sealed class SitranAbrirCetranRequest
{
    public string NumeroAit { get; set; } =
        string.Empty;

    public string NumeroProcesso { get; set; } =
        string.Empty;

    public DateOnly DataAbertura { get; set; }

    public string Operador { get; set; } =
        string.Empty;
}


public sealed class SitranCetranResponse
{
    public string NumeroAit { get; set; } =
        string.Empty;

    public string NumeroProcesso { get; set; } =
        string.Empty;

    public DateOnly? DataAbertura { get; set; }

    public DateTime? DataCadastro { get; set; }

    public string? UsuarioCadastro { get; set; }

    public SitranResultadoProcessoResponse? Resultado { get; set; }

    public DateTime? DataResultado { get; set; }

    public DateTime? DataBaixa { get; set; }

    public string? UsuarioBaixa { get; set; }

    public DateTime? DataPublicacao { get; set; }

    public DateTime? CancelavelAte { get; set; }
}


internal sealed class SitranAbrirJariRequest
{
    public string NumeroAit { get; set; } =
        string.Empty;

    public string NumeroProcesso { get; set; } =
        string.Empty;

    public DateOnly DataAbertura { get; set; }

    public string Operador { get; set; } =
        string.Empty;
}


public sealed class SitranJariResponse
{
    public string NumeroAit { get; set; } =
        string.Empty;

    public string NumeroProcesso { get; set; } =
        string.Empty;

    public DateOnly? DataAbertura { get; set; }

    public DateTime? DataCadastro { get; set; }

    public string? UsuarioCadastro { get; set; }

    public SitranResultadoProcessoResponse? Resultado { get; set; }

    public DateTime? DataResultado { get; set; }

    public DateTime? DataBaixa { get; set; }

    public string? UsuarioBaixa { get; set; }

    public DateTime? DataPublicacao { get; set; }

    public DateTime? CancelavelAte { get; set; }
}



internal sealed class SitranAbrirCondutorRequest
{
    public string NumeroAit { get; set; } =
        string.Empty;

    public string NumeroProcesso { get; set; } =
        string.Empty;

    public DateOnly DataApresentacao { get; set; }

    public SitranCondutorRequest Condutor { get; set; } =
        new();

    public SitranEnderecoCondutorRequest? Endereco { get; set; }

    public string Operador { get; set; } =
        string.Empty;
}


internal sealed class SitranCondutorRequest
{
    public string Nome { get; set; } =
        string.Empty;

    public string RegistroCnh { get; set; } =
        string.Empty;

    public string UfCnh { get; set; } =
        string.Empty;

    public byte ModeloCnh { get; set; }
}


internal sealed class SitranEnderecoCondutorRequest
{
    public string? Logradouro { get; set; }

    public string? Numero { get; set; }

    public string? Complemento { get; set; }

    public string? Bairro { get; set; }

    public string? CodigoMunicipio { get; set; }

    public string? Cep { get; set; }

    public string? Uf { get; set; }
}


public sealed class SitranCondutorResponse
{
    public string NumeroAit { get; set; } =
        string.Empty;

    public string NumeroProcesso { get; set; } =
        string.Empty;

    public DateOnly? DataApresentacao { get; set; }

    public DateTime? DataCadastro { get; set; }

    public string? UsuarioCadastro { get; set; }

    public SitranCondutorDadosResponse? Condutor { get; set; }

    public string? RetornoDetran { get; set; }

    public SitranResultadoProcessoResponse? Resultado { get; set; }

    public DateTime? DataResultado { get; set; }

    public DateTime? DataBaixa { get; set; }

    public string? UsuarioBaixa { get; set; }

    public DateTime? DataPublicacao { get; set; }

    public DateTime? CancelavelAte { get; set; }
}


public sealed class SitranCondutorDadosResponse
{
    public string? Nome { get; set; }

    public string? RegistroCnh { get; set; }

    public string? UfCnh { get; set; }

    public byte? ModeloCnh { get; set; }
}

public sealed class SitranDefesaResponse
{
    public string NumeroAit { get; set; } =
        string.Empty;

    public string NumeroProcesso { get; set; } =
        string.Empty;

    public DateOnly? DataAbertura { get; set; }

    public DateTime? DataCadastro { get; set; }

    public string? UsuarioCadastro { get; set; }

    public SitranResultadoProcessoResponse? Resultado { get; set; }

    public DateTime? DataResultado { get; set; }

    public DateTime? DataBaixa { get; set; }

    public string? UsuarioBaixa { get; set; }

    public DateTime? DataPublicacao { get; set; }

    public bool EfeitoSuspensivoAplicado { get; set; }

    public DateTime? CancelavelAte { get; set; }
}

public sealed class SitranResultadoProcessoResponse
{
    public string? Codigo { get; set; }

    public string? Descricao { get; set; }
}

public sealed class SitranApiException : Exception
{
    public SitranApiException(
        HttpStatusCode statusCode,
        string? codigo,
        string message)
        : base(message)
    {
        StatusCode = statusCode;
        Codigo = codigo;
    }

    public HttpStatusCode StatusCode { get; }

    public string? Codigo { get; }
}

public sealed class SitranElegibilidadeResponse
{
    public string NumeroAit { get; set; } =
        string.Empty;

    public string Situacao { get; set; } =
        string.Empty;

    public bool PagoComDesconto40 { get; set; }

    public SitranRitosElegibilidadeResponse Ritos { get; set; } =
        new();
}

public sealed class SitranRitosElegibilidadeResponse
{
    public SitranRitoElegibilidadeResponse Defesa { get; set; } =
        new();

    public SitranRitoElegibilidadeResponse Condutor { get; set; } =
        new();

    public SitranRitoElegibilidadeResponse Jari { get; set; } =
        new();

    public SitranRitoElegibilidadeResponse Cetran { get; set; } =
        new();
}

public sealed class SitranRitoElegibilidadeResponse
{
    public bool Permitido { get; set; }

    public string? Codigo { get; set; }

    public string? Motivo { get; set; }
}

internal sealed class SitranProblemDetailsResponse
{
    public string? Type { get; set; }

    public string? Title { get; set; }

    public int? Status { get; set; }

    public string? Detail { get; set; }

    public string? Instance { get; set; }

    public string? Codigo { get; set; }
}

internal sealed class SitranAbrirDefesaRequest
{
    public string NumeroAit { get; set; } =
        string.Empty;

    public string NumeroProcesso { get; set; } =
        string.Empty;

    public DateOnly DataAbertura { get; set; }
}

internal sealed class SitranAitResponse
{
    public string? NumeroAit { get; set; }

    public string? Placa { get; set; }

    public SitranInfracaoResponse? Infracao { get; set; }

    public SitranSituacaoResponse? Situacao { get; set; }

    public SitranVeiculoResponse? Veiculo { get; set; }

    public SitranProprietarioResponse? Proprietario { get; set; }

    public SitranPrazosResponse? Prazos { get; set; }

    public SitranValoresResponse? Valores { get; set; }
}

internal sealed class SitranInfracaoResponse
{
    public string? Codigo { get; set; }

    public string? Descricao { get; set; }

    public DateTime? Data { get; set; }

    [JsonPropertyName("local")]
    public string? Local { get; set; }

    public string? Referencia { get; set; }
}

internal sealed class SitranSituacaoResponse
{
    public string? Codigo { get; set; }

    public string? Descricao { get; set; }
}

internal sealed class SitranVeiculoResponse
{
    public string? Renavam { get; set; }

    public string? Marca { get; set; }
}

internal sealed class SitranProprietarioResponse
{
    public string? Nome { get; set; }

    public string? Documento { get; set; }
}

internal sealed class SitranPrazosResponse
{
    public DateTime? DefesaNai { get; set; }

    public DateTime? Apresentacao { get; set; }

    public DateTime? VencimentoNip { get; set; }
}

internal sealed class SitranValoresResponse
{
    public decimal? Cheio { get; set; }
}
