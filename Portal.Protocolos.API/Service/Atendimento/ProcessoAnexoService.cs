using Portal.Protocolos.API.Models.Atendimento.Anexo;
using Portal.Protocolos.API.Repositories;
using Portal.Protocolos.Shared.Exceptions;
using System.Data;

namespace Portal.Protocolos.API.Service
{
    public class ProcessoAnexoService
    {
        private readonly ProcessoAnexoRepository _repository;
        private readonly IWebHostEnvironment _environment;

        private const long TamanhoMaximoBytes = 3 * 1024 * 1024;

        public ProcessoAnexoService(
            ProcessoAnexoRepository repository,
            IWebHostEnvironment environment)
        {
            _repository = repository;
            _environment = environment;
        }

        public async Task<ProcessoAnexoModel> InserirAsync(
            long processoId,
            IFormFile arquivo,
            string? tipoDocumento)
        {
            ValidarArquivo(
                processoId,
                arquivo);

            string? caminhoCompleto = null;

            try
            {
                var anexo =
                    await CriarArquivoFisicoAsync(
                        processoId,
                        arquivo,
                        tipoDocumento);

                caminhoCompleto =
                    ObterCaminhoCompleto(
                        anexo.CaminhoArquivo);

                anexo.ProcessoAnexoId =
                    await _repository.InserirAsync(
                        anexo);

                return anexo;
            }
            catch
            {
                ExcluirArquivoFisico(
                    caminhoCompleto);

                throw;
            }
        }

        public async Task<ProcessoAnexoModel> InserirAsync(
            long processoId,
            IFormFile arquivo,
            string? tipoDocumento,
            IDbConnection connection,
            IDbTransaction transaction,
            bool permitirImagens = false)
        {
            ValidarArquivo(
                processoId,
                arquivo,
                permitirImagens);

            string? caminhoCompleto = null;

            try
            {
                var anexo =
                    await CriarArquivoFisicoAsync(
                        processoId,
                        arquivo,
                        tipoDocumento);

                caminhoCompleto =
                    ObterCaminhoCompleto(
                        anexo.CaminhoArquivo);

                anexo.ProcessoAnexoId =
                    await _repository.InserirAsync(
                        anexo,
                        connection,
                        transaction);

                return anexo;
            }
            catch
            {
                ExcluirArquivoFisico(
                    caminhoCompleto);

                throw;
            }
        }


        public async Task<ProcessoAnexoModel> InserirAsync(
            long processoId,
            string numeroProcesso,
            IFormFile arquivo,
            string? tipoDocumento,
            IDbConnection connection,
            IDbTransaction transaction,
            bool permitirImagens = false)
        {
            ValidarArquivo(
                processoId,
                arquivo,
                permitirImagens);

            var pastaProcesso =
                NormalizarNumeroProcessoParaPasta(
                    numeroProcesso);

            string? caminhoCompleto = null;

            try
            {
                var anexo =
                    await CriarArquivoFisicoAsync(
                        processoId,
                        pastaProcesso,
                        arquivo,
                        tipoDocumento);

                caminhoCompleto =
                    ObterCaminhoCompleto(
                        anexo.CaminhoArquivo);

                anexo.ProcessoAnexoId =
                    await _repository.InserirAsync(
                        anexo,
                        connection,
                        transaction);

                return anexo;
            }
            catch
            {
                ExcluirArquivoFisico(
                    caminhoCompleto);

                throw;
            }
        }

        public async Task<IEnumerable<ProcessoAnexoModel>>
            ListarPorProcessoAsync(
                long processoId)
        {
            if (processoId <= 0)
            {
                throw new BusinessException(
                    "Processo inválido.");
            }

            return await _repository
                .ListarPorProcessoAsync(
                    processoId);
        }

        public async Task ExcluirAsync(
            long processoAnexoId)
        {
            if (processoAnexoId <= 0)
            {
                throw new BusinessException(
                    "Anexo inválido.");
            }

            var anexo =
                await _repository.ObterAsync(
                    processoAnexoId);

            if (anexo == null)
            {
                throw new BusinessException(
                    "Anexo não encontrado.");
            }

            await _repository.ExcluirAsync(
                processoAnexoId);
        }

        public async Task<ProcessoAnexoModel> ObterAsync(
            long processoAnexoId)
        {
            if (processoAnexoId <= 0)
            {
                throw new BusinessException(
                    "Anexo inválido.");
            }

            var anexo =
                await _repository.ObterAsync(
                    processoAnexoId);

            if (anexo == null)
            {
                throw new BusinessException(
                    "Anexo não encontrado.");
            }

            return anexo;
        }

        public void RemoverArquivoFisico(
            ProcessoAnexoModel? anexo)
        {
            if (anexo == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(
                    anexo.CaminhoArquivo))
            {
                return;
            }

            var caminhoCompleto =
                ObterCaminhoCompleto(
                    anexo.CaminhoArquivo);

            ExcluirArquivoFisico(
                caminhoCompleto);
        }


        public void RemoverPastaProcesso(
            string? numeroProcesso)
        {
            if (string.IsNullOrWhiteSpace(
                    numeroProcesso))
            {
                return;
            }

            var pastaProcesso =
                NormalizarNumeroProcessoParaPasta(
                    numeroProcesso);

            var pasta =
                Path.Combine(
                    _environment.ContentRootPath,
                    "Uploads",
                    "Processos",
                    pastaProcesso);

            if (!Directory.Exists(pasta))
            {
                return;
            }

            try
            {
                Directory.Delete(
                    pasta,
                    true);
            }
            catch
            {
                // Não mascara a exceção original
                // que provocou o rollback.
            }
        }

        public void RemoverPastaProcesso(
            long processoId)
        {
            if (processoId <= 0)
            {
                return;
            }

            var pasta =
                Path.Combine(
                    _environment.ContentRootPath,
                    "Uploads",
                    "Processos",
                    processoId.ToString());

            if (!Directory.Exists(pasta))
            {
                return;
            }

            try
            {
                Directory.Delete(
                    pasta,
                    true);
            }
            catch
            {
                // Não mascara a exceção original
                // que provocou o rollback.
            }
        }


        private async Task<ProcessoAnexoModel>
            CriarArquivoFisicoAsync(
                long processoId,
                string pastaProcesso,
                IFormFile arquivo,
                string? tipoDocumento)
        {
            var extensao =
                Path.GetExtension(
                        arquivo.FileName)
                    .ToLowerInvariant();

            var nomeArquivo =
                $"{Guid.NewGuid():N}{extensao}";

            var pasta =
                Path.Combine(
                    _environment.ContentRootPath,
                    "Uploads",
                    "Processos",
                    pastaProcesso);

            Directory.CreateDirectory(
                pasta);

            var caminhoCompleto =
                Path.Combine(
                    pasta,
                    nomeArquivo);

            await using (
                var stream =
                    new FileStream(
                        caminhoCompleto,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None))
            {
                await arquivo.CopyToAsync(
                    stream);
            }

            var caminhoRelativo =
                Path.Combine(
                    "Uploads",
                    "Processos",
                    pastaProcesso,
                    nomeArquivo);

            return new ProcessoAnexoModel
            {
                ProcessoId = processoId,
                NomeArquivo = nomeArquivo,
                NomeOriginal = arquivo.FileName,
                Extensao = extensao,
                TamanhoBytes = arquivo.Length,
                CaminhoArquivo = caminhoRelativo,
                TipoDocumento = tipoDocumento,
                Ativo = true
            };
        }

        private async Task<ProcessoAnexoModel>
            CriarArquivoFisicoAsync(
                long processoId,
                IFormFile arquivo,
                string? tipoDocumento)
        {
            var extensao =
                Path.GetExtension(
                        arquivo.FileName)
                    .ToLowerInvariant();

            var nomeArquivo =
                $"{Guid.NewGuid():N}{extensao}";

            var pasta =
                Path.Combine(
                    _environment.ContentRootPath,
                    "Uploads",
                    "Processos",
                    processoId.ToString());

            Directory.CreateDirectory(
                pasta);

            var caminhoCompleto =
                Path.Combine(
                    pasta,
                    nomeArquivo);

            await using (
                var stream =
                    new FileStream(
                        caminhoCompleto,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None))
            {
                await arquivo.CopyToAsync(
                    stream);
            }

            var caminhoRelativo =
                Path.Combine(
                    "Uploads",
                    "Processos",
                    processoId.ToString(),
                    nomeArquivo);

            return new ProcessoAnexoModel
            {
                ProcessoId = processoId,

                NomeArquivo =
                    nomeArquivo,

                NomeOriginal =
                    arquivo.FileName,

                Extensao =
                    extensao,

                TamanhoBytes =
                    arquivo.Length,

                CaminhoArquivo =
                    caminhoRelativo,

                TipoDocumento =
                    tipoDocumento,

                Ativo = true
            };
        }

        private static void ValidarArquivo(
            long processoId,
            IFormFile arquivo,
            bool permitirImagens = false)
        {
            if (processoId <= 0)
            {
                throw new BusinessException(
                    "Processo inválido.");
            }

            if (arquivo == null ||
                arquivo.Length == 0)
            {
                throw new BusinessException(
                    "Arquivo não informado.");
            }

            if (arquivo.Length >
                TamanhoMaximoBytes)
            {
                throw new BusinessException(
                    "O arquivo não pode ultrapassar 3 MB.");
            }

            var extensao =
                Path.GetExtension(
                        arquivo.FileName)
                    .ToLowerInvariant();

            if (extensao == ".pdf") return;

            if (!permitirImagens)
                throw new BusinessException(
                    "Somente arquivos PDF são permitidos.");

            // Atendimento presencial: imagens digitalizadas e PDF, até 3 MB.
            if (extensao is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                throw new BusinessException(
                    "Somente arquivos PDF, JPG, PNG ou WEBP são permitidos no atendimento presencial.");

            using var stream = arquivo.OpenReadStream();
            Span<byte> cabecalho = stackalloc byte[12];
            var lidos = stream.Read(cabecalho);
            var valido = extensao switch
            {
                ".jpg" or ".jpeg" => lidos >= 3 &&
                    cabecalho[0] == 0xFF && cabecalho[1] == 0xD8 && cabecalho[2] == 0xFF,
                ".png" => lidos >= 8 &&
                    cabecalho[..8].SequenceEqual(new byte[]
                        { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".webp" => lidos >= 12 &&
                    cabecalho[..4].SequenceEqual("RIFF"u8) &&
                    cabecalho[8..12].SequenceEqual("WEBP"u8),
                _ => false
            };
            if (!valido)
                throw new BusinessException(
                    "O conteúdo da imagem não corresponde à extensão do arquivo.");
        }


        private static string NormalizarNumeroProcessoParaPasta(
            string? numeroProcesso)
        {
            var pasta =
                new string(
                    (numeroProcesso ?? string.Empty)
                        .Where(char.IsLetterOrDigit)
                        .Select(char.ToUpperInvariant)
                        .ToArray());

            if (string.IsNullOrWhiteSpace(pasta))
            {
                throw new BusinessException(
                    "Número do processo inválido para armazenamento dos anexos.");
            }

            return pasta;
        }

        private string ObterCaminhoCompleto(
            string caminhoRelativo)
        {
            return Path.Combine(
                _environment.ContentRootPath,
                caminhoRelativo);
        }

        private static void ExcluirArquivoFisico(
            string? caminhoCompleto)
        {
            if (string.IsNullOrWhiteSpace(
                    caminhoCompleto))
            {
                return;
            }

            if (!File.Exists(
                    caminhoCompleto))
            {
                return;
            }

            try
            {
                File.Delete(
                    caminhoCompleto);
            }
            catch
            {
                // Não mascara a exceção original
                // que provocou o rollback.
            }
        }
    }
}
