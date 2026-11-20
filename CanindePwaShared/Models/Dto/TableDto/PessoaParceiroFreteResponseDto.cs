namespace CanindePwaShared.Models.Dto.TableDto
{
    // A tabela tblCdPessoaParceiroFrete não tem created_at/updated_at.
    public class PessoaParceiroFreteResponseDto
    {
        public int ID_PessoaParceiroFrete { get; set; }
        public int ID_Pessoa { get; set; }
    }
}
