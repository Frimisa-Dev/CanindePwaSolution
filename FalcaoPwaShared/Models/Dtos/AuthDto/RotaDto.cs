namespace FalcaoPwaShared.Models.Dtos.AuthDto
{
    public class RotaDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Icone { get; set; }
        public string Caminho { get; set; } = string.Empty;
        public int OrdemGlobal { get; set; }
        public bool Ativo { get; set; }
    }

}
