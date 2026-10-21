using FalcaoPwaShared.Models.Dtos.TableDto;

namespace FalcaoWebServer.Services
{
    public interface IPessoaService
    {
        Task<(IEnumerable<object> items, int total)> GetAllAsync(int page, int pageSize, string? search = null);
        Task<object?> GetByIdAsync(int id);

        Task<int> CreatePessoaAsync(TblPessoaDto dto);

        Task<int> UpdatePessoaAsync(int id, TblPessoaDto dto);

    }
}