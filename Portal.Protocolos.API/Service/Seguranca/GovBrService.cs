using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Portal.Protocolos.API.Models.Seguranca.GovBr;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Portal.Protocolos.API.Service.Seguranca;

public sealed class GovBrService
{
    private readonly GovBrSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;

    public GovBrService(
        IOptions<GovBrSettings> options,
        IMemoryCache cache,
        HttpClient httpClient)
    {
        _settings = options.Value;
        _cache = cache;
        _httpClient = httpClient;
    }

    public string GerarUrlAutorizacao()
    {
        if (string.IsNullOrWhiteSpace(
                _settings.BaseUrl))
        {
            throw new InvalidOperationException(
                "BaseUrl do Gov.br não configurada.");
        }

        if (string.IsNullOrWhiteSpace(
                _settings.ClientId))
        {
            throw new InvalidOperationException(
                "ClientId do Gov.br não configurado.");
        }

        if (string.IsNullOrWhiteSpace(
                _settings.RedirectUri))
        {
            throw new InvalidOperationException(
                "RedirectUri do Gov.br não configurado.");
        }

        var state =
            GerarValorAleatorio(32);

        var nonce =
            GerarValorAleatorio(32);

        var codeVerifier =
            GerarCodeVerifier();

        var codeChallenge =
            GerarCodeChallenge(
                codeVerifier);

        var authState =
            new GovBrAuthState
            {
                State = state,
                Nonce = nonce,
                CodeVerifier = codeVerifier,
                ExpiraEm =
                    DateTime.UtcNow
                        .AddMinutes(10)
            };

        _cache.Set(
            GerarChaveCache(state),
            authState,
            TimeSpan.FromMinutes(10));

        var parametros =
            new Dictionary<string, string?>
            {
                ["response_type"] =
                    "code",

                ["client_id"] =
                    _settings.ClientId,

                ["scope"] =
                    _settings.Scope,

                ["redirect_uri"] =
                    _settings.RedirectUri,

                ["nonce"] =
                    nonce,

                ["state"] =
                    state,

                ["code_challenge"] =
                    codeChallenge,

                ["code_challenge_method"] =
                    "S256"
            };

        var queryString =
            string.Join(
                "&",
                parametros.Select(
                    x =>
                        $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value ?? string.Empty)}"));

        return
            $"{_settings.BaseUrl.TrimEnd('/')}/authorize?{queryString}";
    }

    public GovBrAuthState ObterEstado(
        string state)
    {
        if (string.IsNullOrWhiteSpace(
                state))
        {
            throw new InvalidOperationException(
                "State do Gov.br não informado.");
        }

        var chave =
            GerarChaveCache(state);

        if (!_cache.TryGetValue(
                chave,
                out GovBrAuthState? authState) ||
            authState is null)
        {
            throw new InvalidOperationException(
                "State do Gov.br inválido ou expirado.");
        }

        if (authState.ExpiraEm <
            DateTime.UtcNow)
        {
            _cache.Remove(
                chave);

            throw new InvalidOperationException(
                "State do Gov.br expirado.");
        }

        return authState;
    }

    public async Task<GovBrTokenResponse>
        TrocarCodePorTokenAsync(
            string code,
            string state)
    {
        if (string.IsNullOrWhiteSpace(
                code))
        {
            throw new InvalidOperationException(
                "Code do Gov.br não informado.");
        }

        if (string.IsNullOrWhiteSpace(
                _settings.ClientSecret))
        {
            throw new InvalidOperationException(
                "ClientSecret do Gov.br não configurado.");
        }

        var authState =
            ObterEstado(state);

        var credenciais =
            $"{_settings.ClientId}:{_settings.ClientSecret}";

        var basicToken =
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    credenciais));

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"{_settings.BaseUrl.TrimEnd('/')}/token");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Basic",
                basicToken);

        request.Content =
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["grant_type"] =
                        "authorization_code",

                    ["code"] =
                        code,

                    ["redirect_uri"] =
                        _settings.RedirectUri,

                    ["code_verifier"] =
                        authState.CodeVerifier
                });

        using var response =
            await _httpClient.SendAsync(
                request);

        var conteudo =
            await response.Content
                .ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Não foi possível obter o token do Gov.br. Status: {(int)response.StatusCode}.");
        }

        var token =
            JsonSerializer.Deserialize<GovBrTokenResponse>(
                conteudo,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive =
                        true
                });

        if (token is null ||
            string.IsNullOrWhiteSpace(
                token.AccessToken))
        {
            throw new InvalidOperationException(
                "Resposta de token inválida do Gov.br.");
        }

        return token;
    }

    public async Task<GovBrUserInfo>
        ObterUsuarioAsync(
            string accessToken)
    {
        if (string.IsNullOrWhiteSpace(
                accessToken))
        {
            throw new InvalidOperationException(
                "Access token do Gov.br não informado.");
        }

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"{_settings.BaseUrl.TrimEnd('/')}/userinfo/");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        using var response =
            await _httpClient.SendAsync(
                request);

        var conteudo =
            await response.Content
                .ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Não foi possível obter os dados do usuário no Gov.br. Status: {(int)response.StatusCode}.");
        }

        var usuario =
            JsonSerializer.Deserialize<GovBrUserInfo>(
                conteudo,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive =
                        true
                });

        if (usuario is null ||
            string.IsNullOrWhiteSpace(
                usuario.Cpf))
        {
            throw new InvalidOperationException(
                "O Gov.br não retornou um usuário válido.");
        }

        return usuario;
    }

    public string CriarTokenCadastro(
        GovBrUserInfo usuario)
    {
        if (string.IsNullOrWhiteSpace(
                usuario.Cpf))
        {
            throw new InvalidOperationException(
                "Não foi possível identificar o CPF autenticado no Gov.br.");
        }

        var tokenCadastro =
            GerarValorAleatorio(48);

        var cadastroState =
            new GovBrCadastroState
            {
                TokenCadastro =
                    tokenCadastro,

                Cpf =
                    SomenteNumeros(
                        usuario.Cpf),

                Nome =
                    usuario.Nome,

                NomeSocial =
                    usuario.NomeSocial,

                Email =
                    usuario.Email,

                EmailVerificado =
                    usuario.EmailVerificado,

                Telefone =
                    usuario.Telefone,

                TelefoneVerificado =
                    usuario.TelefoneVerificado,

                ExpiraEm =
                    DateTime.UtcNow
                        .AddMinutes(20)
            };

        _cache.Set(
            GerarChaveCadastro(
                tokenCadastro),
            cadastroState,
            TimeSpan.FromMinutes(20));

        return tokenCadastro;
    }

    public GovBrCadastroState
        ObterCadastro(
            string tokenCadastro)
    {
        if (string.IsNullOrWhiteSpace(
                tokenCadastro))
        {
            throw new InvalidOperationException(
                "Token de cadastro Gov.br não informado.");
        }

        var chave =
            GerarChaveCadastro(
                tokenCadastro);

        if (!_cache.TryGetValue(
                chave,
                out GovBrCadastroState?
                    cadastroState) ||
            cadastroState is null)
        {
            throw new InvalidOperationException(
                "Cadastro Gov.br inválido ou expirado.");
        }

        if (cadastroState.ExpiraEm <
            DateTime.UtcNow)
        {
            _cache.Remove(
                chave);

            throw new InvalidOperationException(
                "O prazo para concluir o cadastro Gov.br expirou.");
        }

        return cadastroState;
    }

    public void RemoverCadastro(
        string tokenCadastro)
    {
        if (string.IsNullOrWhiteSpace(
                tokenCadastro))
        {
            return;
        }

        _cache.Remove(
            GerarChaveCadastro(
                tokenCadastro));
    }

    public void RemoverEstado(
        string state)
    {
        if (string.IsNullOrWhiteSpace(
                state))
        {
            return;
        }

        _cache.Remove(
            GerarChaveCache(state));
    }

    private static string
        GerarCodeVerifier()
    {
        return
            GerarValorAleatorio(64);
    }

    private static string
        GerarCodeChallenge(
            string codeVerifier)
    {
        var bytes =
            Encoding.ASCII.GetBytes(
                codeVerifier);

        var hash =
            SHA256.HashData(
                bytes);

        return
            Base64UrlEncode(
                hash);
    }

    private static string
        GerarValorAleatorio(
            int tamanhoBytes)
    {
        var bytes =
            RandomNumberGenerator
                .GetBytes(
                    tamanhoBytes);

        return
            Base64UrlEncode(
                bytes);
    }

    private static string
        Base64UrlEncode(
            byte[] bytes)
    {
        return
            Convert
                .ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
    }

    private static string
        GerarChaveCache(
            string state)
    {
        return
            $"govbr:auth:{state}";
    }

    private static string
        GerarChaveCadastro(
            string tokenCadastro)
    {
        return
            $"govbr:cadastro:{tokenCadastro}";
    }

    private static string
        SomenteNumeros(
            string valor)
    {
        return new string(
            valor
                .Where(char.IsDigit)
                .ToArray());
    }
}
