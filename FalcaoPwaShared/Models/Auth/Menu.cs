using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FalcaoPwaShared.Models.Auth
{
    [Table("menus")]
    public class Menu
    {
        [Key, Column(Order = 0)]
        public int PerfilId { get; set; }

        [Key, Column(Order = 1)]
        public int RotaId { get; set; }

        public int? Ordem { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // NAVIGATION PROPERTIES - Inicializadas com default!
        [ForeignKey(nameof(RotaId))]
        public virtual Rota Rota { get; set; } = default!;

        [ForeignKey(nameof(PerfilId))]
        public virtual Perfil Perfil { get; set; } = default!;
    }
}