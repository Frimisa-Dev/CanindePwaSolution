namespace CanindePwaShared.Models.Dto.TableDto
{
    public class PessoaProdutorRuralResponseDto
    {
        public int ID_PessoaProdutorRural { get; set; }
        public int ID_Pessoa { get; set; }
        public string? Pis { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
