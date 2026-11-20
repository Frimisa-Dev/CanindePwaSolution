using CanindePwaShared.Enums;

namespace CanindePwaShared.Models.Dto.TableDto
{
    public class TblPessoaResponseDto
    {
        public int ID_Pessoa { get; set; }

        public string TipoPessoa { get; set; } = string.Empty;   // "Fisica" | "Juridica"

        public string? Cpf { get; set; }
        public string? Rg { get; set; }
        public string? Cnpj { get; set; }
        public string? InscricaoEstadual { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Cep { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string ComplementoNumero { get; set; } = string.Empty;

        public string DddCelular { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contato { get; set; } = string.Empty;

        // Perfis que a pessoa possui (vazio = pessoa sem perfil).
        public List<string> Perfis { get; set; } = new();

        public EstatusEnum Estatus { get; set; }
    }
}
