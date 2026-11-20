using System.ComponentModel.DataAnnotations;

namespace CanindePwaShared.Models.Dto.TableDto
{
    public class TblBairroRequestDto
    {
        [Required]
        public required string Descricao { get; set; }
    }
}