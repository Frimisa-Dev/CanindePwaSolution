using FalcaoPwaShared.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace FalcaoPwaShared.Models.Tbl
{
    public class TblPessoaEmail
    {
        public int ID_PessoaEmail { get; set; }
        public int ID_Pessoa { get; set; }
        [Column("Setor")]
        public SetorEnum Setor { get; set; }
        public string? Email { get; set; }
        public SimNaoEnum SelecionadoParaImprimir { get; set; }
    }
}

