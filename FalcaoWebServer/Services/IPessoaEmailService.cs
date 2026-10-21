using FalcaoPwaShared.Models.Dtos.TableDto;

namespace FalcaoWebServer.Services
{
    public interface IPessoaEmailService
    {
        Task<IEnumerable<TblPessoaEmailDto>> GetAllAsync(int idPessoa);
        Task<TblPessoaEmailDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(TblPessoaEmailDto dto);
        Task UpdateAsync(TblPessoaEmailDto dto);
        Task DeleteAsync(int id);
    }
}


