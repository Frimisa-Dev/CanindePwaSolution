namespace CanindePwaShared.Models.Dto.AuthDto
{
    public class PerfilDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty; // "CEO", "ADMIN", etc.
        public string? Descricao { get; set; }
        public int NivelAcesso { get; set; }
        public bool Ativo { get; set; }
    }
}
