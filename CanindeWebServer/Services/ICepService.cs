using CanindePwaShared.Models.Dto.TableDto;
using CanindePwaShared.Models.Tbl;

namespace CanindeWebServer.Services
{
    public interface ICepService
    {
        /// <summary>
        /// Devolve o CEP cadastrado em tblcep; se não existir, consulta o ViaCEP e cadastra
        /// (logradouro, bairro e cidade são reaproveitados por descrição; o estado vem SEMPRE de tblestado).
        /// Deve ser chamado ANTES de abrir a transação da pessoa.
        /// </summary>
        Task<TblCep> ResolverAsync(string cep, string? logradouro, string? bairro, string? cidade, string? uf,
                                   CancellationToken ct = default);

        /// <summary>Consulta um CEP (banco primeiro, depois ViaCEP) sem gravar nada.</summary>
        Task<CepResponseDto?> ConsultarAsync(string cep, CancellationToken ct = default);
    }
}
