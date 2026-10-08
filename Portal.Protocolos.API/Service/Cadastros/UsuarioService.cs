using Portal.Protocolos.API.Models.Seguranca.GovBr;
using Portal.Protocolos.API.Models.Seguranca.Login;
using Portal.Protocolos.API.Models.Seguranca.Usuario;
using Portal.Protocolos.API.Models.Seguranca.UsuarioAutenticacao;
using Portal.Protocolos.API.Models.Seguranca.UsuarioPerfil;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Service.Seguranca;
using Portal.Protocolos.API.Validators;
using Portal.Protocolos.Shared.Constants;
using Portal.Protocolos.Shared.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace Portal.Protocolos.API.Service.Cadastros;

public sealed class UsuarioService
{
    private readonly UsuarioRepository
        _usuarioRepository;

    private readonly UsuarioAutenticacaoRepository
        _autenticacaoRepository;

    private readonly UsuarioPerfilRepository
        _usuarioPerfilRepository;

    private readonly GovBrService
        _govBrService;

    public UsuarioService(
        UsuarioRepository usuarioRepository,
        UsuarioAutenticacaoRepository autenticacaoRepository,
        UsuarioPerfilRepository usuarioPerfilRepository,
        GovBrService govBrService)
    {
        _usuarioRepository =
            usuarioRepository;

        _autenticacaoRepository =
            autenticacaoRepository;

        _usuarioPerfilRepository =
            usuarioPerfilRepository;

        _govBrService =
            govBrService;
    }

    public async Task<UsuarioMeusDadosResponse>
    AtualizarMeusDadosAsync(
        int usuarioId,
        UsuarioMeusDadosAtualizarRequest request)
    {
        var usuario = await _usuarioRepository
            .ObterMeusDadosAsync(usuarioId);

        if (usuario is null)
        {
            throw new BusinessException(
                "Usuário não encontrado.");
        }

        var pessoaFisica =
            usuario.TipoPessoaId == TipoPessoaConstants.Fisica;

        var pessoaJuridica =
            usuario.TipoPessoaId == TipoPessoaConstants.Juridica;

        if (!pessoaFisica && !pessoaJuridica)
        {
            throw new BusinessException(
                "Tipo de pessoa inválido no cadastro.");
        }

        var dados = new UsuarioMeusDadosAtualizarRequest
        {
            Nome = request.Nome?.Trim() ?? string.Empty,

            NomeFantasia =
                string.IsNullOrWhiteSpace(request.NomeFantasia)
                    ? null
                    : request.NomeFantasia.Trim(),

            TipoPessoaJuridicaId = request.TipoPessoaJuridicaId,

            Email = request.Email?.Trim().ToLowerInvariant()
                ?? string.Empty,

            Celular = SomenteNumeros(request.Celular),

            RegistroCnh =
                string.IsNullOrWhiteSpace(request.RegistroCnh)
                    ? null
                    : request.RegistroCnh.Trim(),

            UfCnh =
                string.IsNullOrWhiteSpace(request.UfCnh)
                    ? null
                    : request.UfCnh.Trim().ToUpperInvariant(),

            ModeloCnh = request.ModeloCnh
        };

        if (string.IsNullOrEmpty(dados.Celular))
        {
            dados.Celular = null;
        }

        if (pessoaFisica &&
            (dados.TipoPessoaJuridicaId.HasValue ||
             dados.NomeFantasia is not null))
        {
            throw new BusinessException(
                "Pessoa física não deve informar nome fantasia ou tipo de pessoa jurídica.");
        }

        if (pessoaJuridica)
        {
            if (dados.TipoPessoaJuridicaId is not 1 and not 2)
            {
                throw new BusinessException(
                    "Informe se a pessoa jurídica é Empresa Privada ou Órgão/Entidade Pública.");
            }

            if (dados.RegistroCnh is not null ||
                dados.UfCnh is not null ||
                dados.ModeloCnh.HasValue)
            {
                throw new BusinessException(
                    "Dados de CNH devem ser informados somente para pessoa física.");
            }
        }

        var validator =
            new UsuarioMeusDadosAtualizarRequestValidator();

        var validacao = await validator.ValidateAsync(dados);

        if (!validacao.IsValid)
        {
            throw new BusinessException(
                validacao.Errors[0].ErrorMessage);
        }

        var emailEmUso = await _usuarioRepository
            .ExisteEmailOutroUsuarioAsync(
                dados.Email,
                usuarioId);

        if (emailEmUso)
        {
            throw new BusinessException(
                "Já existe outro usuário cadastrado com este e-mail.");
        }

        var atualizado = await _usuarioRepository
            .AtualizarMeusDadosAsync(usuarioId, dados);

        if (!atualizado)
        {
            throw new BusinessException(
                "Não foi possível atualizar os dados do usuário.");
        }

        return await ObterMeusDadosAsync(usuarioId);
    }





    public async Task CriarAsync(
        UsuarioCadastroRequest request)
    {
        var documento =
            SomenteNumeros(
                request.Documento);

        var celular =
            SomenteNumeros(
                request.Celular);

        var email =
            request.Email
                .Trim()
                .ToLowerInvariant();

        if (
            await _usuarioRepository
                .ExisteDocumentoAsync(
                    documento))
        {
            throw new BusinessException(
                "Já existe um usuário cadastrado com este CPF ou CNPJ.");
        }

        if (
            await _usuarioRepository
                .ExisteEmailAsync(
                    email))
        {
            throw new BusinessException(
                "Já existe um usuário cadastrado com este e-mail.");
        }

        var tipoPessoaId =
            documento.Length == 11
                ? TipoPessoaConstants.Fisica
                : TipoPessoaConstants.Juridica;

        short? tipoPessoaJuridicaId =
            null;

        if (
            tipoPessoaId ==
            TipoPessoaConstants.Juridica)
        {
            if (
                request.TipoPessoaJuridicaId
                    is not 1
                    and not 2)
            {
                throw new BusinessException(
                    "Informe se a pessoa jurídica é Empresa Privada ou Órgão/Entidade Pública.");
            }

            tipoPessoaJuridicaId =
                request.TipoPessoaJuridicaId;
        }

        string? registroCnh =
    null;

        string? ufCnh =
            null;

        byte? modeloCnh =
            null;

        if (
            tipoPessoaId ==
            TipoPessoaConstants.Fisica)
        {
            var possuiRegistroCnh =
                !string.IsNullOrWhiteSpace(
                    request.RegistroCnh);

            var possuiUfCnh =
                !string.IsNullOrWhiteSpace(
                    request.UfCnh);

            var possuiModeloCnh =
                request.ModeloCnh.HasValue;

            var informouAlgumDadoCnh =
                possuiRegistroCnh ||
                possuiUfCnh ||
                possuiModeloCnh;

            if (informouAlgumDadoCnh)
            {
                if (!possuiRegistroCnh)
                {
                    throw new BusinessException(
                        "Informe o número da CNH.");
                }

                if (!possuiUfCnh)
                {
                    throw new BusinessException(
                        "Informe a UF da CNH.");
                }

                if (!possuiModeloCnh)
                {
                    throw new BusinessException(
                        "Informe o modelo da CNH.");
                }

                registroCnh =
                    request.RegistroCnh!
                        .Trim();

                ufCnh =
                    request.UfCnh!
                        .Trim()
                        .ToUpperInvariant();

                modeloCnh =
                    request.ModeloCnh;

                if (registroCnh.Length > 20)
                {
                    throw new BusinessException(
                        "O número da CNH deve possuir no máximo 20 caracteres.");
                }

                if (ufCnh.Length != 2)
                {
                    throw new BusinessException(
                        "Informe uma UF válida para a CNH.");
                }

                if (
                    modeloCnh != 2 &&
                    modeloCnh != 3)
                {
                    throw new BusinessException(
                        "Modelo da CNH inválido.");
                }
            }
        }

        var usuario =
            new UsuarioModel
            {
                TipoPessoaId =
                    tipoPessoaId,

                TipoPessoaJuridicaId =
                    tipoPessoaJuridicaId,

                StatusUsuarioId =
                    StatusUsuarioConstants
                        .PendenteConfirmacao,

                Nome =
                    request.Nome.Trim(),

                NomeFantasia =
                    tipoPessoaId ==
                        TipoPessoaConstants
                            .Juridica &&
                    !string.IsNullOrWhiteSpace(
                        request.NomeFantasia)
                        ? request
                            .NomeFantasia
                            .Trim()
                        : null,

                Documento =
                    documento,

                Email =
                    email,

                Celular =
                    string.IsNullOrWhiteSpace(
                        celular)
                        ? null
                        : celular,

   

                RegistroCnh =
                    registroCnh,

                UfCnh =
                    ufCnh,

                ModeloCnh =
                    modeloCnh
            };

        var usuarioId =
            await _usuarioRepository
                .InserirAsync(
                    usuario);

        var autenticacao =
            new UsuarioAutenticacao
            {
                UsuarioId =
                    usuarioId,

                TipoAutenticacaoId =
                    TipoAutenticacaoConstants
                        .Local,

                SenhaHash =
                    BCrypt.Net.BCrypt
                        .HashPassword(
                            request.Senha),

                TentativasFalhas = 0,

                Ativo = true
            };

        await _autenticacaoRepository
            .InserirAsync(
                autenticacao);

        var usuarioPerfil =
            new UsuarioPerfil
            {
                UsuarioId =
                    usuarioId,

                PerfilId =
                    PerfilConstants.Cidadao,

                Ativo = true
            };

        await _usuarioPerfilRepository
            .InserirAsync(
                usuarioPerfil);
    }


    public async Task<UsuarioMeusDadosResponse>
    ObterMeusDadosAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository
            .ObterMeusDadosAsync(usuarioId);

        if (usuario is null)
        {
            throw new BusinessException(
                "Usuário não encontrado.");
        }

        return usuario;
    }



    public async Task<LoginUsuarioModel> ConcluirCadastroGovBrAsync(GovBrConcluirCadastroRequest request)
    {
        if (string.IsNullOrWhiteSpace(
                request.TokenCadastro))
        {
            throw new BusinessException(
                "Token de cadastro Gov.br não informado.");
        }

        var cadastroGovBr =
            _govBrService
                .ObterCadastro(
                    request.TokenCadastro);

        var cpfGovBr =
            SomenteNumeros(
                cadastroGovBr.Cpf);

        if (cpfGovBr.Length != 11)
        {
            throw new BusinessException(
                "O CPF autenticado pelo Gov.br é inválido.");
        }

        if (
            request.TipoPessoaId !=
                TipoPessoaConstants.Fisica &&
            request.TipoPessoaId !=
                TipoPessoaConstants.Juridica)
        {
            throw new BusinessException(
                "Tipo de pessoa inválido.");
        }

        string documento;
        string nome;
        string? nomeFantasia;
        short? tipoPessoaJuridicaId;

        string? registroCnh = null;
        string? ufCnh = null;
        byte? modeloCnh = null;

        if (
            request.TipoPessoaId ==
            TipoPessoaConstants.Fisica)
        {
            documento =
                cpfGovBr;

            nome =
                !string.IsNullOrWhiteSpace(
                    cadastroGovBr.NomeSocial)
                    ? cadastroGovBr
                        .NomeSocial
                        .Trim()
                    : cadastroGovBr
                        .Nome
                        .Trim();

            nomeFantasia =
                null;

            tipoPessoaJuridicaId =
                null;

            var possuiRegistroCnh =
                !string.IsNullOrWhiteSpace(
                    request.RegistroCnh);

            var possuiUfCnh =
                !string.IsNullOrWhiteSpace(
                    request.UfCnh);

            var possuiModeloCnh =
                request.ModeloCnh.HasValue;

            var informouAlgumDadoCnh =
                possuiRegistroCnh ||
                possuiUfCnh ||
                possuiModeloCnh;

            if (informouAlgumDadoCnh)
            {
                if (!possuiRegistroCnh)
                {
                    throw new BusinessException(
                        "Informe o número da CNH.");
                }

                if (!possuiUfCnh)
                {
                    throw new BusinessException(
                        "Informe a UF da CNH.");
                }

                if (!possuiModeloCnh)
                {
                    throw new BusinessException(
                        "Informe o modelo da CNH.");
                }

                registroCnh =
                    request.RegistroCnh!
                        .Trim();

                ufCnh =
                    request.UfCnh!
                        .Trim()
                        .ToUpperInvariant();

                modeloCnh =
                    request.ModeloCnh;

                if (registroCnh.Length > 20)
                {
                    throw new BusinessException(
                        "O número da CNH deve possuir no máximo 20 caracteres.");
                }

                if (ufCnh.Length != 2)
                {
                    throw new BusinessException(
                        "Informe uma UF válida para a CNH.");
                }

                if (
                    modeloCnh != 2 &&
                    modeloCnh != 3)
                {
                    throw new BusinessException(
                        "Modelo da CNH inválido.");
                }
            }
        }
        else
        {
            documento =
                SomenteNumeros(
                    request.Cnpj);

            if (documento.Length != 14)
            {
                throw new BusinessException(
                    "Informe um CNPJ válido.");
            }

            if (string.IsNullOrWhiteSpace(
                    request.RazaoSocial))
            {
                throw new BusinessException(
                    "Informe a razão social.");
            }

            if (
                request.TipoPessoaJuridicaId
                    is not 1
                    and not 2)
            {
                throw new BusinessException(
                    "Informe se a pessoa jurídica é Empresa Privada ou Órgão/Entidade Pública.");
            }

            nome =
                request.RazaoSocial
                    .Trim();

            nomeFantasia =
                string.IsNullOrWhiteSpace(
                    request.NomeFantasia)
                    ? null
                    : request
                        .NomeFantasia
                        .Trim();

            tipoPessoaJuridicaId =
                request.TipoPessoaJuridicaId;
        }

        var email =
            request.Email
                .Trim()
                .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(
                email))
        {
            email =
                cadastroGovBr.Email?
                    .Trim()
                    .ToLowerInvariant() ??
                string.Empty;
        }

        if (string.IsNullOrWhiteSpace(
                email))
        {
            throw new BusinessException(
                "Informe o e-mail.");
        }

        var celular =
            SomenteNumeros(
                request.Celular);

        if (string.IsNullOrWhiteSpace(
                celular))
        {
            celular =
                SomenteNumeros(
                    cadastroGovBr.Telefone);
        }

        if (
            await _usuarioRepository
                .ExisteDocumentoAsync(
                    documento))
        {
            throw new BusinessException(
                "Já existe um usuário cadastrado com este CPF ou CNPJ.");
        }

        if (
            await _usuarioRepository
                .ExisteEmailAsync(
                    email))
        {
            throw new BusinessException(
                "Já existe um usuário cadastrado com este e-mail.");
        }

        var usuario =
            new UsuarioModel
            {
                TipoPessoaId =
                    request.TipoPessoaId,

                TipoPessoaJuridicaId =
                    tipoPessoaJuridicaId,

                StatusUsuarioId =
                    StatusUsuarioConstants
                        .PendenteConfirmacao,

                Nome =
                    nome,

                NomeFantasia =
                    nomeFantasia,

                Documento =
                    documento,

                Email =
                    email,

                Celular =
                    string.IsNullOrWhiteSpace(
                        celular)
                        ? null
                        : celular,

                RegistroCnh =
                    registroCnh,

                UfCnh =
                    ufCnh,

                ModeloCnh =
                    modeloCnh
            };

        var usuarioId =
            await _usuarioRepository
                .InserirAsync(
                    usuario);

        var autenticacao =
            new UsuarioAutenticacao
            {
                UsuarioId =
                    usuarioId,

                TipoAutenticacaoId =
                    TipoAutenticacaoConstants
                        .GovBr,

                SenhaHash =
                    null,

                IdentificadorExterno =
                    cpfGovBr,

                TentativasFalhas =
                    0,

                Ativo =
                    true
            };

        await _autenticacaoRepository
            .InserirAsync(
                autenticacao);

        var usuarioPerfil =
            new UsuarioPerfil
            {
                UsuarioId =
                    usuarioId,

                PerfilId =
                    PerfilConstants.Cidadao,

                Ativo =
                    true
            };

        await _usuarioPerfilRepository
            .InserirAsync(
                usuarioPerfil);

        _govBrService
            .RemoverCadastro(
                request.TokenCadastro);

        return new LoginUsuarioModel
        {
            UsuarioId =
                usuarioId,

            TipoPessoaId =
                usuario.TipoPessoaId,

            TipoPessoaJuridicaId =
                usuario.TipoPessoaJuridicaId,

            Nome =
                usuario.Nome,

            Documento =
                usuario.Documento,

            Email =
                usuario.Email,

            SenhaHash =
                string.Empty,

            Ativo =
                true
        };
    }
    public async Task AlterarSenhaAsync(int usuarioId,AlterarSenhaRequest request)
    {
        if (
            string.IsNullOrWhiteSpace(
                request.SenhaAtual))
        {
            throw new BusinessException(
                "Informe a senha atual.");
        }

        if (
            string.IsNullOrWhiteSpace(
                request.NovaSenha))
        {
            throw new BusinessException(
                "Informe a nova senha.");
        }

        if (
            request.NovaSenha.Length < 8)
        {
            throw new BusinessException(
                "A nova senha deve possuir no mínimo 8 caracteres.");
        }

        if (
            request.NovaSenha !=
            request.ConfirmarNovaSenha)
        {
            throw new BusinessException(
                "A confirmação da nova senha não confere.");
        }

        if (
            request.SenhaAtual ==
            request.NovaSenha)
        {
            throw new BusinessException(
                "A nova senha deve ser diferente da senha atual.");
        }

        var senhaHashAtual =
            await _autenticacaoRepository
                .ObterSenhaHashAsync(
                    usuarioId);

        if (
            string.IsNullOrWhiteSpace(
                senhaHashAtual))
        {
            throw new BusinessException(
                "Não foi possível localizar a autenticação do usuário.");
        }

        if (
            !ValidarSenha(
                request.SenhaAtual,
                senhaHashAtual))
        {
            throw new BusinessException(
                "A senha atual está incorreta.");
        }

        var novaSenhaHash =
            BCrypt.Net.BCrypt
                .HashPassword(
                    request.NovaSenha);

        var atualizado =
            await _autenticacaoRepository
                .AtualizarSenhaAsync(
                    usuarioId,
                    novaSenhaHash);

        if (!atualizado)
        {
            throw new BusinessException(
                "Não foi possível alterar a senha.");
        }
    }

    private static bool ValidarSenha(string senha, string senhaHash)
    {
        if (
            string.IsNullOrWhiteSpace(
                senhaHash))
        {
            return false;
        }

        if (
            senhaHash.StartsWith("$2a$") ||
            senhaHash.StartsWith("$2b$") ||
            senhaHash.StartsWith("$2y$"))
        {
            try
            {
                return BCrypt.Net.BCrypt
                    .Verify(
                        senha,
                        senhaHash);
            }
            catch
            {
                return false;
            }
        }

        var senhaMd5 =
            GerarMd5(
                senha);

        return senhaMd5.Equals(
            senhaHash,
            StringComparison
                .OrdinalIgnoreCase);
    }

    private static string GerarMd5(string senha)
    {
        var bytes =
            MD5.HashData(
                Encoding.UTF8.GetBytes(
                    senha));

        return Convert
            .ToHexString(bytes)
            .ToLowerInvariant();
    }

    private static string SomenteNumeros(
        string? valor)
    {
        if (
            string.IsNullOrWhiteSpace(
                valor))
        {
            return string.Empty;
        }

        return new string(
            valor
                .Where(char.IsDigit)
                .ToArray());
    }
}
