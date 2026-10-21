namespace FalcaoPwaShared.Models.Dtos.AuthDto
{
    public class MenuResponseDto
    {
        public List<MenuItemDto> Itens { get; set; } = new();
        public string PerfilCodigo { get; set; } = string.Empty;
    }
}
