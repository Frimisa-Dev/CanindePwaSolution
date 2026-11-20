using CanindePwaShared.Models.Dto.TableDto;

namespace CanindeWebServer.Services
{
    public interface IPessoaService
    {
        /// <summary>Lista paginada. Filtros: texto (nome/razão social/CPF/CNPJ) e perfil.</summary>
        Task<(IReadOnlyList<TblPessoaResponseDto> items, int total)> GetAllAsync(
            int page, int pageSize, string? search, string? perfil, CancellationToken ct = default);

        Task<TblPessoaResponseDto?> GetByIdAsync(int id, CancellationToken ct = default);

        /// <summary>Cria pessoa + física/jurídica + e-mail + perfis, em uma transação. Devolve o ID_Pessoa.</summary>
        Task<int> CreatePessoaAsync(TblPessoaRequestDto dto, CancellationToken ct = default);

        /// <summary>Atualiza dados básicos. Tipo da pessoa e CPF/CNPJ não mudam por aqui.</summary>
        Task UpdatePessoaAsync(int id, TblPessoaRequestDto dto, CancellationToken ct = default);

        /// <summary>Inativa a pessoa (Estatus = Desativado no banco). Nunca apaga.</summary>
        Task InativarAsync(int id, CancellationToken ct = default);

        Task ReativarAsync(int id, CancellationToken ct = default);

        Task<IReadOnlyList<string>> GetPerfisAsync(int id, CancellationToken ct = default);

        /// <summary>Adiciona um perfil a uma pessoa já cadastrada, respeitando a regra de combinação.</summary>
        Task AdicionarPerfilAsync(int id, string perfil, PessoaPerfilRequestDto? dados, CancellationToken ct = default);
    }
}
