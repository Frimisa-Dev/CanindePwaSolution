using FalcaoPwaShared.Enums;

namespace FalcaoPwaShared.Models.Uvw
{
    public class UvwPessoaEmail
    {
        public int ID_PessoaEmail { get; set; }
        public int ID_Pessoa { get; set; }
        public string? NomePessoa { get; set; }
        public SetorEnum Setor { get; set; }
        public string? Email { get; set; }
        public SimNaoEnum SelecionadoParaImprimir { get; set; }
    }
}
