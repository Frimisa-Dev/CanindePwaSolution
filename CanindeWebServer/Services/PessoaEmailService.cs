#nullable enable
using CanindePwaShared.Enums;
using CanindePwaShared.Models.Dto.TableDto;
using CanindePwaShared.Models.Tbl;
using CanindeWebServer.Data;
using CanindeWebServer.Services.Utils;
using Microsoft.EntityFrameworkCore;

namespace CanindeWebServer.Services
{
    public class PessoaEmailService : IPessoaEmailService
    {
        private readonly ProjetoDbContext _db;
        private readonly ILogger<PessoaEmailService> _logger;

        public PessoaEmailService(ProjetoDbContext db, ILogger<PessoaEmailService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<TblPessoaEmailResponseDto>> GetAllAsync(int idPessoa)
        {
            var emails = await _db.TblPessoaEmail
                .Where(e => e.ID_Pessoa == idPessoa)
                .AsNoTracking()
                .Select(e => new TblPessoaEmailResponseDto               // ← CORRIGIDO: ResponseDto
                {
                    ID_PessoaEmail = e.ID_PessoaEmail,
                    ID_Pessoa = e.ID_Pessoa,
                    Setor = e.Setor.ToString(),
                    Email = e.Email ?? string.Empty,
                    SelecionadoParaImprimir = e.SelecionadoParaImprimir  // ← CORRIGIDO: sem .ToString()
                })
                .ToListAsync()
                .ConfigureAwait(false);

            return emails;
        }

        public async Task<TblPessoaEmailResponseDto?> GetByIdAsync(int id)
        {
            var e = await _db.TblPessoaEmail.FindAsync(id).ConfigureAwait(false);
            if (e == null) return null;

            return new TblPessoaEmailResponseDto                          // ← CORRIGIDO: ResponseDto
            {
                ID_PessoaEmail = e.ID_PessoaEmail,
                ID_Pessoa = e.ID_Pessoa,
                Setor = e.Setor.ToString(),
                Email = e.Email ?? string.Empty,
                SelecionadoParaImprimir = e.SelecionadoParaImprimir       // ← CORRIGIDO: sem .ToString()
            };
        }

        public async Task<int> CreateAsync(TblPessoaEmailRequestDto dto)
        {
            using var tx = await _db.Database.BeginTransactionAsync().ConfigureAwait(false);

            try
            {
                var entity = new TblPessoaEmail
                {
                    ID_Pessoa = dto.ID_Pessoa,
                    Email = dto.Email,
                    Setor = UtilizarEnum.ParseOrDefault(dto.Setor, SetorEnum.Cadastro),
                    SelecionadoParaImprimir = dto.SelecionadoParaImprimir     // ← CORRIGIDO: atribuição direta
                };

                _db.TblPessoaEmail.Add(entity);
                await _db.SaveChangesAsync().ConfigureAwait(false);
                await tx.CommitAsync().ConfigureAwait(false);

                return entity.ID_PessoaEmail;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                _logger.LogError(ex, "Erro ao criar PessoaEmail");
                throw;
            }
        }

        public async Task UpdateAsync(TblPessoaEmailRequestDto dto)
        {
            try
            {
                var e = await _db.TblPessoaEmail.FindAsync(dto.ID_PessoaEmail).ConfigureAwait(false);
                if (e == null)
                    throw new KeyNotFoundException($"PessoaEmail ID {dto.ID_PessoaEmail} não encontrado.");

                if (!string.IsNullOrWhiteSpace(dto.Setor))
                    e.Setor = UtilizarEnum.ParseOrDefault(dto.Setor, e.Setor);

                e.Email = dto.Email;

                e.SelecionadoParaImprimir = dto.SelecionadoParaImprimir;  // ← CORRIGIDO: atribuição direta, sem bloco if

                await _db.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar PessoaEmail ID={Id}", dto.ID_PessoaEmail);
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var e = await _db.TblPessoaEmail.FindAsync(id).ConfigureAwait(false);
                if (e == null)
                    return;

                _db.TblPessoaEmail.Remove(e);
                await _db.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir PessoaEmail ID={Id}", id);
                throw;
            }
        }
    }
}