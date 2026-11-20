using System.ComponentModel.DataAnnotations;

namespace CanindePwaShared.Models.Dto.TableDto
{
    public class PessoaProdutorRuralRequestDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "ID_Pessoa é obrigatório.")]
        public int ID_Pessoa { get; set; }

        // Opcional. A coluna é VARCHAR(20).
        [StringLength(20, ErrorMessage = "Pis deve ter no máximo 20 caracteres.")]
        public string? Pis { get; set; }
    }
}
