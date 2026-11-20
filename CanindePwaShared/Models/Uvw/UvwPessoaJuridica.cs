using CanindePwaShared.Enums;

namespace CanindePwaShared.Models.Uvw
{
    public class UvwPessoaJuridica
    {
        public int ID_PessoaJuridica { get; set; }
        public int ID_Pessoa { get; set; } = 0;
        public string? RazaoSocial { get; set; } = string.Empty;
        public string? Cnpj { get; set; } = string.Empty;
        public string? InscricaoEstadual { get; set; } = string.Empty;
        public DateTime DataFundacao { get; set; }
        public string? DddFixo { get; set; } = string.Empty;
        public string? Fixo { get; set; } = string.Empty;
        public string? DddCelular { get; set; } = string.Empty;
        public string? Celular { get; set; } = string.Empty;
        public EstatusEnum Estatus { get; set; }
    }
}
