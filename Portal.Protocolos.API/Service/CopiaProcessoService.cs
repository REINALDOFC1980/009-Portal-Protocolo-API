using Portal.Protocolos.API.Models.Atendimento.Copia;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.API.Service;
using Portal.Protocolos.Shared.Exceptions;

namespace Portal.Protocolos.API.Services.Atendimento;

public sealed class CopiaProcessoService
{
    private readonly CopiaProcessoRepository _repository;
    private readonly ProcessoAnexoService _anexos;
    private readonly IWebHostEnvironment _environment;

    public CopiaProcessoService(
        CopiaProcessoRepository repository,
        ProcessoAnexoService anexos,
        IWebHostEnvironment environment)
    {
        _repository = repository;
        _anexos = anexos;
        _environment = environment;
    }

    public async Task<CopiaProcessoResponse?> ConsultarAsync(
        string? numeroProcesso)
    {
        var numero = NormalizarNumeroProcesso(numeroProcesso);

        var processo = await _repository.ObterPorNumeroAsync(numero);

        if (processo is null)
            return null;

        processo.Descricao = DescricaoServico(
            processo.CodigoServico,
            processo.Descricao);

        var anexos = await _anexos.ListarPorProcessoAsync(
            processo.ProcessoId);

        processo.Anexos = anexos
            .OrderBy(a => a.DataCadastro)
            .ThenBy(a => a.ProcessoAnexoId)
            .Select(a => new CopiaProcessoAnexoResponse
            {
                ProcessoAnexoId = a.ProcessoAnexoId,
                NomeOriginal = a.NomeOriginal,
                Extensao = a.Extensao,
                TipoDocumento = a.TipoDocumento,
                DataCadastro = a.DataCadastro,
                Imagem = EhImagem(a.Extensao)
            })
            .ToList();

        return processo;
    }

    public async Task<CopiaProcessoArquivoResponse> ObterArquivoAsync(
        long processoAnexoId)
    {
        if (processoAnexoId <= 0)
            throw new BusinessException("Anexo inválido.");

        var anexo = await _anexos.ObterAsync(processoAnexoId);

        var raiz = Path.GetFullPath(_environment.ContentRootPath);
        var relativo = (anexo.CaminhoArquivo ?? string.Empty)
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        var caminho = Path.GetFullPath(Path.Combine(raiz, relativo));

        var raizComSeparador = raiz.EndsWith(Path.DirectorySeparatorChar)
            ? raiz
            : raiz + Path.DirectorySeparatorChar;

        if (!caminho.StartsWith(
                raizComSeparador,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("Caminho do anexo inválido.");
        }

        if (!File.Exists(caminho))
            throw new BusinessException("Arquivo físico do anexo não encontrado.");

        return new CopiaProcessoArquivoResponse
        {
            Conteudo = await File.ReadAllBytesAsync(caminho),
            ContentType = ObterContentType(anexo.Extensao),
            NomeArquivo = string.IsNullOrWhiteSpace(anexo.NomeOriginal)
                ? Path.GetFileName(caminho)
                : anexo.NomeOriginal
        };
    }

    private static string NormalizarNumeroProcesso(string? valor)
    {
        var numero = (valor ?? string.Empty).Trim().ToUpperInvariant();

        if (numero.Length is < 3 or > 40)
            throw new BusinessException("Informe um número de processo válido.");

        return numero;
    }

    private static bool EhImagem(string? extensao) =>
        (extensao ?? string.Empty).Trim().ToLowerInvariant()
            is ".jpg" or ".jpeg" or ".png" or ".webp";

    private static string ObterContentType(string? extensao) =>
        (extensao ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };

    private static string DescricaoServico(
        string? codigo,
        string? descricaoBanco)
    {
        return (codigo ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "CONDUTOR" => "Condutor Infrator",
            "DEFESA" => "Defesa Prévia",
            "JARI" => "Recurso JARI",
            _ => string.IsNullOrWhiteSpace(descricaoBanco)
                ? "Processo Administrativo"
                : descricaoBanco.Trim()
        };
    }
}
