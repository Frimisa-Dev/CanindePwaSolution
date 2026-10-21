using FalcaoPwaShared.Enums;
using System.ComponentModel.DataAnnotations;

namespace FalcaoPwaShared.Models.Dtos.TableDto
{
    public class TblPessoaDto
    {
        [Required]
        [RegularExpression("Fisica|Juridica", ErrorMessage = "TipoPessoa deve ser 'Fisica' ou 'Juridica'")]
        public required string TipoPessoa { get; set; }

        // PESSOA FÍSICA
        [RegularExpression(@"\d{3}\.\d{3}\.\d{3}-\d{2}", ErrorMessage = "CPF deve estar no formato 000.000.000-00")]
        public string? Cpf { get; set; }

        public string? Rg { get; set; }

        // PESSOA JURÍDICA
        [RegularExpression(@"\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}", ErrorMessage = "CNPJ deve estar no formato 00.000.000/0000-00")]
        public string? Cnpj { get; set; }

        public string? InscricaoEstadual { get; set; }

        [Required]
        public required string Nome { get; set; }

        [Required]
        [RegularExpression(@"\d{5}-\d{3}", ErrorMessage = "CEP deve estar no formato 00000-000")]
        public required string Cep { get; set; }

        public string Logradouro { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;

        [Required]
        public required string Numero { get; set; }

        public string ComplementoNumero { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"\d{2}", ErrorMessage = "DDD deve conter 2 dígitos")]
        public required string DddCelular { get; set; }

        [Required]
        [Phone]
        public required string Celular { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Contato { get; set; }

        [Required]
        public required string Perfil { get; set; }

        public decimal Comissao { get; set; }
        public EstatusEnum Estatus { get; set; }
    }
}