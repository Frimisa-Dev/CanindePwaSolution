using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FalcaoPwaShared.Models.Auth
{
    [Table("rotas")]
    public class Rota
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Icone { get; set; }

        [Required]
        [MaxLength(100)]
        public string Caminho { get; set; } = string.Empty;

        public int OrdemGlobal { get; set; } = 0;

        public bool Ativo { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
