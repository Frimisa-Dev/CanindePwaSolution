using CanindePwaShared.Enums;

namespace CanindePwaShared.Models.Dto.TableDto
{
    public class TblPessoaEmailResponseDto
    {
        public int ID_PessoaEmail { get; set; }
        public int ID_Pessoa { get; set; }
        public string Setor { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public SimNaoEnum SelecionadoParaImprimir { get; set; }
    }
}