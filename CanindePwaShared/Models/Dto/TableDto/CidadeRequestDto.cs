using System.ComponentModel.DataAnnotations;

namespace CanindePwaShared.Models.Dto.TableDto
{
    public class CidadeRequestDto
    {
        [Required]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Descricao deve ter entre 1 e 50 caracteres.")]
        public required string Descricao { get; set; }
    }
}