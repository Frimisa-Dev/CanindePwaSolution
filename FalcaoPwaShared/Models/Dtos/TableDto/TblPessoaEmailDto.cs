using System.ComponentModel.DataAnnotations;

namespace FalcaoPwaShared.Models.Dtos.TableDto
{
    public class TblPessoaEmailDto
    {
        public int ID_PessoaEmail { get; set; }
        public int ID_Pessoa { get; set; }

        [Required]
        public required string Setor { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        public string SelecionadoParaImprimir { get; set; } = "N";
    }
}