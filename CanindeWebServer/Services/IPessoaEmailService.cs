using CanindePwaShared.Models.Dto.TableDto;

namespace CanindeWebServer.Services
{
    public interface IPessoaEmailService
    {
        Task<IEnumerable<TblPessoaEmailResponseDto>> GetAllAsync(int idPessoa);
        Task<TblPessoaEmailResponseDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(TblPessoaEmailRequestDto dto);
        Task UpdateAsync(TblPessoaEmailRequestDto dto);
        Task DeleteAsync(int id);
    }
}


