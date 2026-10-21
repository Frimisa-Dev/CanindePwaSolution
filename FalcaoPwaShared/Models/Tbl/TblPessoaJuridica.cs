namespace FalcaoPwaShared.Models.Tbl
{
    public class TblPessoaJuridica
    {
        public int ID_PessoaJuridica { get; set; }
        public int ID_Pessoa { get; set; } = 0;
        public string? RazaoSocial { get; set; } = string.Empty;
        public string? Cnpj { get; set; } = string.Empty;
        public string? InscricaoEstadual { get; set; } = string.Empty;
        public DateTime DataFundacao { get; set; }
    }
}
