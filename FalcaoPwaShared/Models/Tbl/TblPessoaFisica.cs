namespace FalcaoPwaShared.Models.Tbl
{
    public class TblPessoaFisica
    {
        public int ID_PessoaFisica { get; set; }
        public int ID_Pessoa { get; set; } = 0;
        public string? Nome { get; set; } = string.Empty;
        public string? Cpf { get; set; } = string.Empty;
        public string? Rg { get; set; } = string.Empty;
        public DateTime DataNascimento { get; set; }
    }
}
