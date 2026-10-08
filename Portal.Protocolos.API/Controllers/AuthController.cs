using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Portal.Protocolos.API.Models.Seguranca.GovBr;
using Portal.Protocolos.API.Models.Seguranca.Login;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Service.Seguranca;

namespace Portal.Protocolos.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const string CodigoGovBrSimulado =
        "GOVBR_SIMULACAO_CODE";

    private const string StateGovBrSimulado =
        "GOVBR_SIMULACAO_STATE";

    private readonly LoginService
        _loginService;

    private readonly JwtService
        _jwtService;

    private readonly GovBrService
        _govBrService;

    private readonly UsuarioRepository
        _usuarioRepository;

    private readonly UsuarioAutenticacaoRepository
        _usuarioAutenticacaoRepository;

    private readonly GovBrSettings
        _govBrSettings;

    public AuthController(
        LoginService loginService,
        JwtService jwtService,
        GovBrService govBrService,
        UsuarioRepository usuarioRepository,
        UsuarioAutenticacaoRepository usuarioAutenticacaoRepository,
        IOptions<GovBrSettings> govBrOptions)
    {
        _loginService =
            loginService;

        _jwtService =
            jwtService;

        _govBrService =
            govBrService;

        _usuarioRepository =
            usuarioRepository;

        _usuarioAutenticacaoRepository =
            usuarioAutenticacaoRepository;

        _govBrSettings =
            govBrOptions.Value;
    }

    /*
     * LOGIN LOCAL
     *
     * Continua funcionando normalmente.
     * Por enquanto NÃO será removido.
     */
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request)
    {
        var usuario =
            await _loginService
                .AutenticarAsync(
                    request);

        var response =
            _jwtService
                .GerarToken(
                    usuario);

        return Ok(response);
    }

    /*
     * LOGIN INTERNO
     *
     * Atendimento / Analista /
     * Supervisor / Administrador.
     */
    [HttpPost("interno/login")]
    public async Task<IActionResult> LoginInterno(
        LoginRequest request)
    {
        var (usuario, perfis) =
            await _loginService
                .AutenticarInternoAsync(
                    request);

        var response =
            _jwtService
                .GerarTokenInterno(
                    usuario,
                    perfis);

        return Ok(response);
    }

    /*
     * INÍCIO DO LOGIN GOV.BR
     *
     * Se ModoSimulacao = true:
     * não acessa nenhum serviço externo.
     *
     * Se ModoSimulacao = false:
     * utiliza o Gov.br verdadeiro.
     */
    [HttpGet("govbr")]
    public IActionResult GovBr()
    {
        if (_govBrSettings.ModoSimulacao)
        {
            var frontEndUrl =
                _govBrSettings
                    .FrontEndUrl?
                    .TrimEnd('/');

            if (string.IsNullOrWhiteSpace(
                    frontEndUrl))
            {
                throw new InvalidOperationException(
                    "FrontEndUrl do Gov.br não configurado.");
            }

            var url =
                $"{frontEndUrl}/govbr/callback" +
                $"?code={Uri.EscapeDataString(CodigoGovBrSimulado)}" +
                $"&state={Uri.EscapeDataString(StateGovBrSimulado)}";

            return Ok(
                new
                {
                    url,
                    simulacao = true
                });
        }

        /*
         * GOV.BR REAL
         */
        var urlGovBr =
            _govBrService
                .GerarUrlAutorizacao();

        return Ok(
            new
            {
                url = urlGovBr,
                simulacao = false
            });
    }

    /*
     * CALLBACK DO GOV.BR
     *
     * O mesmo endpoint é utilizado
     * tanto na simulação quanto no
     * Gov.br verdadeiro.
     */
    [HttpPost("govbr/callback")]
    public async Task<IActionResult> GovBrCallback(
        GovBrCallbackRequest request)
    {
        if (!string.IsNullOrWhiteSpace(
                request.Error))
        {
            return BadRequest(
                new
                {
                    message =
                        request.ErrorDescription ??
                        $"Autenticação Gov.br não concluída. Erro: {request.Error}"
                });
        }

        if (string.IsNullOrWhiteSpace(
                request.Code))
        {
            return BadRequest(
                new
                {
                    message =
                        "O Gov.br não retornou o código de autorização."
                });
        }

        if (string.IsNullOrWhiteSpace(
                request.State))
        {
            return BadRequest(
                new
                {
                    message =
                        "O Gov.br não retornou o state da autenticação."
                });
        }

        /*
         * =====================================================
         * MODO SIMULAÇÃO
         * =====================================================
         *
         * Não chama nenhum endpoint externo.
         *
         * O restante do sistema recebe exatamente
         * o mesmo modelo que receberia do Gov.br.
         */
        if (_govBrSettings.ModoSimulacao)
        {
            if (
                request.Code != CodigoGovBrSimulado ||
                request.State != StateGovBrSimulado)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Retorno inválido da simulação Gov.br."
                    });
            }

            var usuarioGovBrSimulado =
                CriarUsuarioGovBrSimulado();

            return await
                ProcessarUsuarioGovBrAsync(
                    usuarioGovBrSimulado);
        }

        /*
         * =====================================================
         * GOV.BR REAL
         * =====================================================
         */
        try
        {
            var tokenGovBr =
                await _govBrService
                    .TrocarCodePorTokenAsync(
                        request.Code,
                        request.State);

            var usuarioGovBr =
                await _govBrService
                    .ObterUsuarioAsync(
                        tokenGovBr.AccessToken);

            return await
                ProcessarUsuarioGovBrAsync(
                    usuarioGovBr);
        }
        finally
        {
            _govBrService
                .RemoverEstado(
                    request.State);
        }
    }

    /*
     * =========================================================
     * PROCESSAMENTO DO USUÁRIO IDENTIFICADO PELO GOV.BR
     * =========================================================
     *
     * Esse método é propositalmente o mesmo tanto
     * para simulação quanto para produção.
     *
     * Portanto:
     *
     * SIMULAÇÃO
     *      ↓
     * GovBrUserInfo
     *      ↓
     * ProcessarUsuarioGovBrAsync
     *
     * GOV.BR REAL
     *      ↓
     * GovBrUserInfo
     *      ↓
     * ProcessarUsuarioGovBrAsync
     */
    private async Task<IActionResult>
        ProcessarUsuarioGovBrAsync(
            GovBrUserInfo usuarioGovBr)
    {
        var cpfGovBr =
            SomenteNumeros(
                usuarioGovBr.Cpf);

        if (cpfGovBr.Length != 11)
        {
            return BadRequest(
                new
                {
                    message =
                        "O GOV.BR não retornou um CPF válido."
                });
        }

        /*
         * Primeiro procura uma autenticação
         * GOV.BR já vinculada.
         */
        var usuarioLogin =
            await _usuarioAutenticacaoRepository
                .ObterLoginGovBrAsync(
                    cpfGovBr);

        if (usuarioLogin is not null)
        {
            var loginResponse =
                _jwtService
                    .GerarToken(
                        usuarioLogin);

            return Ok(
                new
                {
                    autenticado =
                        true,

                    cadastroCompleto =
                        true,

                    token =
                        loginResponse.Token,

                    expiraEm =
                        loginResponse.ExpiraEm,

                    nome =
                        loginResponse.Nome,

                    documento =
                        loginResponse.Documento,

                    email =
                        loginResponse.Email,

                    tipoPessoaId =
                        loginResponse.TipoPessoaId,

                    tipoPessoaJuridicaId =
                        loginResponse
                            .TipoPessoaJuridicaId
                });
        }

        /*
         * Compatibilidade com usuários
         * que já existiam antes da
         * implantação do Gov.br.
         *
         * Por enquanto mantemos exatamente
         * essa regra para não quebrar os
         * usuários existentes.
         */
        var usuarioLocal =
            await _usuarioRepository
                .ObterPorDocumentoAsync(
                    cpfGovBr);

        if (usuarioLocal is not null)
        {
            var usuarioExistente =
                new LoginUsuarioModel
                {
                    UsuarioId =
                        usuarioLocal.UsuarioId,

                    TipoPessoaId =
                        usuarioLocal.TipoPessoaId,

                    TipoPessoaJuridicaId =
                        usuarioLocal
                            .TipoPessoaJuridicaId,

                    Nome =
                        usuarioLocal.Nome,

                    Documento =
                        usuarioLocal.Documento,

                    Email =
                        usuarioLocal.Email,

                    Ativo =
                        true
                };

            var loginResponse =
                _jwtService
                    .GerarToken(
                        usuarioExistente);

            return Ok(
                new
                {
                    autenticado =
                        true,

                    cadastroCompleto =
                        true,

                    token =
                        loginResponse.Token,

                    expiraEm =
                        loginResponse.ExpiraEm,

                    nome =
                        loginResponse.Nome,

                    documento =
                        loginResponse.Documento,

                    email =
                        loginResponse.Email,

                    tipoPessoaId =
                        loginResponse.TipoPessoaId,

                    tipoPessoaJuridicaId =
                        loginResponse
                            .TipoPessoaJuridicaId
                });
        }

        /*
         * Usuário ainda não existe.
         *
         * Aqui NÃO cadastramos automaticamente.
         *
         * Criamos um token temporário
         * e enviamos os dados identificados
         * pelo Gov.br para a tela de
         * complementação de cadastro.
         */
        var tokenCadastro =
            _govBrService
                .CriarTokenCadastro(
                    usuarioGovBr);

        return Ok(
            new
            {
                autenticado =
                    true,

                cadastroCompleto =
                    false,

                tokenCadastro,

                documento =
                    cpfGovBr,

                nome =
                    usuarioGovBr.Nome,

                nomeSocial =
                    usuarioGovBr.NomeSocial,

                email =
                    usuarioGovBr.Email,

                emailVerificado =
                    usuarioGovBr.EmailVerificado,

                telefone =
                    usuarioGovBr.Telefone,

                telefoneVerificado =
                    usuarioGovBr
                        .TelefoneVerificado
            });
    }

    /*
     * =========================================================
     * USUÁRIO SIMULADO GOV.BR
     * =========================================================
     *
     * Os dados vêm do appsettings.
     *
     * Isso permite testar:
     *
     * 1 - CPF existente
     * 2 - CPF ainda não cadastrado
     * 3 - usuário com ou sem e-mail
     * 4 - usuário com ou sem telefone
     *
     * sem alterar código.
     */
    private GovBrUserInfo
        CriarUsuarioGovBrSimulado()
    {
        var simulacao =
            _govBrSettings.Simulacao;

        return new GovBrUserInfo
        {
            Cpf =
                simulacao.Cpf,

            Nome =
                simulacao.Nome,

            NomeSocial =
                simulacao.NomeSocial,

            Email =
                simulacao.Email,

            EmailVerificado =
                simulacao
                    .EmailVerificado,

            Telefone =
                simulacao.Telefone,

            TelefoneVerificado =
                simulacao
                    .TelefoneVerificado
        };
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
                .Where(
                    char.IsDigit)
                .ToArray());
    }
}
