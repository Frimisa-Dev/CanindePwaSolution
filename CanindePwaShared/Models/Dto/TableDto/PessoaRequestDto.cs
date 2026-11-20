using System.ComponentModel.DataAnnotations;

namespace CanindePwaShared.Models.Dto.TableDto
{
    /// <summary>
    /// Cadastro/edição de pessoa. Limites de tamanho = colunas reais do banco (verificado em 2026-11-19).
    /// O e-mail NÃO fica na tblpessoa: vai para tblpessoaemail (setor Cadastro).
    /// </summary>
    public class TblPessoaRequestDto
    {
        [Required]
        [RegularExpression("(?i)^(Fisica|Juridica)$",
            ErrorMessage = "TipoPessoa deve ser 'Fisica' ou 'Juridica'")]
        public required string TipoPessoa { get; set; }

        // PESSOA FÍSICA
        [RegularExpression(@"\d{3}\.\d{3}\.\d{3}-\d{2}",
            ErrorMessage = "CPF deve estar no formato 000.000.000-00")]
        public string? Cpf { get; set; }

        [StringLength(30)]
        public string? Rg { get; set; }

        // PESSOA JURÍDICA
        [RegularExpression(@"\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}",
            ErrorMessage = "CNPJ deve estar no formato 00.000.000/0000-00")]
        public string? Cnpj { get; set; }

        [StringLength(30)]
        public string? InscricaoEstadual { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Nome deve ter entre 1 e 100 caracteres.")]
        public required string Nome { get; set; }

        [Required]
        [RegularExpression(@"\d{5}-\d{3}",
            ErrorMessage = "CEP deve estar no formato 00000-000")]
        public required string Cep { get; set; }

        // Usados SOMENTE quando o CEP não é encontrado (banco nem ViaCEP).
        [StringLength(100)] public string Logradouro { get; set; } = string.Empty;
        [StringLength(50)] public string Bairro { get; set; } = string.Empty;
        [StringLength(50)] public string Cidade { get; set; } = string.Empty;
        [StringLength(2)] public string Uf { get; set; } = string.Empty;

        [Required]
        [StringLength(10, MinimumLength = 1, ErrorMessage = "Numero deve ter entre 1 e 10 caracteres.")]
        public required string Numero { get; set; }

        [StringLength(45)]
        public string ComplementoNumero { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"\d{2}", ErrorMessage = "DDD deve conter 2 dígitos")]
        public required string DddCelular { get; set; }

        [Required]
        [Phone]
        [StringLength(20)]
        public required string Celular { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public required string Email { get; set; }

        [Required]
        [StringLength(45, MinimumLength = 1, ErrorMessage = "Contato deve ter entre 1 e 45 caracteres.")]
        public required string Contato { get; set; }

        // Perfis: vazio (pessoa sem perfil), um perfil, ou exatamente Embarcador + Agenciador.
        // Valores: Embarcador, Agenciador, Motorista, Corretor, Cliente, Fornecedor, ProdutorRural, ParceiroFrete.
        public List<string> Perfis { get; set; } = new();

        // Dados específicos de perfil
        [StringLength(45)] public string? NomeFantasia { get; set; }   // Fornecedor (obrigatório nesse perfil)
        [StringLength(20)] public string? Pis { get; set; }            // ProdutorRural (opcional)

        // Embarcador, Agenciador, Corretor e ParceiroFrete: usuário (tblusuario) ao qual a pessoa fica ligada.
        public int? ID_Usuario { get; set; }
    }
}
