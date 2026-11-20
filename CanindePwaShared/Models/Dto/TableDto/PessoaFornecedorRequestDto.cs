using System.ComponentModel.DataAnnotations;

namespace CanindePwaShared.Models.Dto.TableDto
{
    public class PessoaFornecedorRequestDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "ID_Pessoa é obrigatório.")]
        public int ID_Pessoa { get; set; }

        [Required]
        [StringLength(45, MinimumLength = 1, ErrorMessage = "NomeFantasia deve ter entre 1 e 45 caracteres.")]
        public required string NomeFantasia { get; set; }
    }
}
