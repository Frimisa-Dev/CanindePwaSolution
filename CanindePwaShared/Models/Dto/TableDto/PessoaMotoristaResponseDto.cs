namespace CanindePwaShared.Models.Dto.TableDto
{
    public class PessoaMotoristaResponseDto
    {
        public int ID_PessoaMotorista { get; set; }
        public int ID_Pessoa { get; set; }
        public int ID_PessoaAgenciador { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
