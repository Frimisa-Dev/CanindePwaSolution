#nullable enable
using FalcaoPwaShared.Enums;
using FalcaoPwaShared.Models.Dtos.TableDto;
using FalcaoPwaShared.Models.Tbl;
using FalcaoWebServer.Data;
using FalcaoWebServer.Services.Utils;
using Microsoft.EntityFrameworkCore;

namespace FalcaoWebServer.Services
{
    public class PessoaEmailService : IPessoaEmailService
    {
        private readonly FalcaoDbContext _db;
        private readonly ILogger<PessoaEmailService> _logger;

        public PessoaEmailService(FalcaoDbContext db, ILogger<PessoaEmailService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<TblPessoaEmailDto>> GetAllAsync(int idPessoa)
        {
            var emails = await _db.TblPessoaEmail
                .Where(e => e.ID_Pessoa == idPessoa)
                .AsNoTracking()
                .Select(e => new TblPessoaEmailDto
                {
                    ID_PessoaEmail = e.ID_PessoaEmail,
                    ID_Pessoa = e.ID_Pessoa,
                    Setor = e.Setor.ToString(),
                    Email = e.Email,
                    SelecionadoParaImprimir = e.SelecionadoParaImprimir.ToString()
                })
                .ToListAsync()
                .ConfigureAwait(false);

            return emails;
        }

        public async Task<TblPessoaEmailDto?> GetByIdAsync(int id)
        {
            var e = await _db.TblPessoaEmail.FindAsync(id).ConfigureAwait(false);
            if (e == null) return null;

            return new TblPessoaEmailDto
            {
                ID_PessoaEmail = e.ID_PessoaEmail,
                ID_Pessoa = e.ID_Pessoa,
                Setor = e.Setor.ToString(),
                Email = e.Email,
                SelecionadoParaImprimir = e.SelecionadoParaImprimir.ToString()
            };
        }

        public async Task<int> CreateAsync(TblPessoaEmailDto dto)
        {
            using var tx = await _db.Database.BeginTransactionAsync().ConfigureAwait(false);

            try
            {
                var entity = new TblPessoaEmail
                {
                    ID_Pessoa = dto.ID_Pessoa,
                    Email = dto.Email,
                    Setor = UtilizarEnum.ParseOrDefault(dto.Setor, SetorEnum.Cadastro),
                    SelecionadoParaImprimir = UtilizarEnum.ParseOrDefault(dto.SelecionadoParaImprimir, SimNaoEnum.Nao)
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

        public async Task UpdateAsync(TblPessoaEmailDto dto)
        {
            try
            {
                var e = await _db.TblPessoaEmail.FindAsync(dto.ID_PessoaEmail).ConfigureAwait(false);
                if (e == null)
                    throw new KeyNotFoundException($"PessoaEmail ID {dto.ID_PessoaEmail} não encontrado.");

                if (!string.IsNullOrWhiteSpace(dto.Setor))
                    e.Setor = UtilizarEnum.ParseOrDefault(dto.Setor, e.Setor);
                e.Email = dto.Email;

                if (!string.IsNullOrWhiteSpace(dto.SelecionadoParaImprimir))
                {
                    e.SelecionadoParaImprimir = UtilizarEnum.ParseOrDefault(dto.SelecionadoParaImprimir, SimNaoEnum.Nao);
                }
                else
                {
                    e.SelecionadoParaImprimir = SimNaoEnum.Nao;
                }

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