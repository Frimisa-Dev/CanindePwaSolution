using FalcaoPwaShared.Enums;

namespace FalcaoPwaShared.Models.Uvw
{
    public class UvwPessoaFisica
    {
        public int ID_PessoaFisica { get; set; }
        public int ID_Pessoa { get; set; } = 0;
        public string? Nome { get; set; } = string.Empty;
        public string? Cpf { get; set; } = string.Empty;
        public string? Rg { get; set; } = string.Empty;
        public DateTime DataNascimento { get; set; }    
        public string? DddFixo { get; set; } = string.Empty;
        public string? Fixo { get; set; } = string.Empty;
        public string? DddCelular { get; set; } = string.Empty;
        public string? Celular { get; set; } = string.Empty;
        public EstatusEnum Estatus { get; set; }
    }
}
