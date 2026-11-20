namespace CanindePwaShared.Models.Dto.AuthDto
{
    public class MenuItemDto
    {
        public int RotaId { get; set; } 
        public string Nome { get; set; } = string.Empty;
        public string? Icone { get; set; }
        public string Caminho { get; set; } = string.Empty;
        public int Ordem { get; set; }
        public bool Ativo { get; set; } = true; 
    }

}
