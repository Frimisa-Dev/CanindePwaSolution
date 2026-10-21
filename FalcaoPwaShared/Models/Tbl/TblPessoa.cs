using FalcaoPwaShared.Enums;

namespace FalcaoPwaShared.Models.Tbl
{
    public class TblPessoa
    {
        public int ID_Pessoa { get; set; }
        public int ID_Cep { get; set; } = 0;
        public int CodigoPessoaTipo { get; set; } = 0;
        public string? Numero { get; set; } = string.Empty;
        public string? ComplementoNumero { get; set; } = string.Empty;
        public string? DddFixo { get; set; } = string.Empty;
        public string? Fixo { get; set; } = string.Empty;
        public string? DddCelular { get; set; } = string.Empty;
        public string? Celular { get; set; } = string.Empty;
        public string? Contato { get; set; } = string.Empty;
        public EstatusEnum Estatus { get; set; }
    }
}