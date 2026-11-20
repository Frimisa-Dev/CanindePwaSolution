namespace CanindePwaShared.Models.Dto.TableDto
{
    public class EstadoResponseDto
    {
        public int ID_Estado { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;
    }
}