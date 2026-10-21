using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FalcaoPwaShared.Models.Auth
{
    [Table("perfis")]
    public class Perfil
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Codigo { get; set; } = string.Empty; // "CEO", "ADMIN", etc.

        [MaxLength(100)]
        public string? Descricao { get; set; }

        public int NivelAcesso { get; set; } = 1; // 1=baixo, 5=alto

        public bool Ativo { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
