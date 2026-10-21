using FalcaoPwaShared.Enums;

namespace FalcaoPwaShared.Models.Uvw
{
    public class UvwPessoaCliente
    {
        public int ID_PessoaCliente { get; set; }
        public int ID_Pessoa { get; set; } = 0;
        public int ID_Cep { get; set; } = 0;
        public string? NomeCliente { get; set; } = string.Empty;
        public int TipoPessoa { get; set; } = 0;
        public string? CpfCnpj { get; set; } = string.Empty;
        public string? RgInscricaoEstadual { get; set; } = string.Empty;
        public DateTime NascimentoFundacao { get; set; }
        public string? Cep { get; set; } = string.Empty;
        public string? Logradouro { get; set; } = string.Empty;
        public string? Numero { get; set; } = string.Empty;
        public string? ComplementoNumero { get; set; } = string.Empty;
        public string? Bairro { get; set; } = string.Empty;
        public string? Cidade { get; set; } = string.Empty;
        public string? Uf { get; set; } = string.Empty;
        public string? DddFixo { get; set; } = string.Empty;
        public string? Fixo { get; set; } = string.Empty;
        public string? DddCelular { get; set; } = string.Empty;  
        public string? Celular { get; set; } = string.Empty;
        public EstatusEnum Estatus { get; set; }
    }
}
