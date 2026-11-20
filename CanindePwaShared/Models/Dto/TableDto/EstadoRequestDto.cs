using System.ComponentModel.DataAnnotations;

namespace CanindePwaShared.Models.Dto.TableDto
{
    public class TblEstadoRequestDto
    {
        [Required]
        [StringLength(25, MinimumLength = 1, ErrorMessage = "Descricao deve ter entre 1 e 25 caracteres.")]
        public required string Descricao { get; set; }

        [Required]
        [RegularExpression(@"^[A-Za-z]{2}$", ErrorMessage = "Uf deve conter 2 letras (ex.: MG).")]
        public required string Uf { get; set; }
    }
}
