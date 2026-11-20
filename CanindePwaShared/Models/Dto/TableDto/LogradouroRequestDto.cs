using System.ComponentModel.DataAnnotations;

namespace CanindePwaShared.Models.Dto.TableDto
{
    public class LogradouroRequestDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Descricao deve ter entre 1 e 100 caracteres.")]
        public required string Descricao { get; set; }
    }
}