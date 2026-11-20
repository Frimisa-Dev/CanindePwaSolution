using CanindePwaShared.Enums;
using System.ComponentModel.DataAnnotations;

namespace CanindePwaShared.Models.Dto.TableDto
{
    public class TblPessoaEmailRequestDto
    {
        // null em Create (server gera), preenchido em Update (bate com o ID da URL)
        public int? ID_PessoaEmail { get; set; }

        [Required]
        public int ID_Pessoa { get; set; }

        [Required]
        public required string Setor { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        public SimNaoEnum SelecionadoParaImprimir { get; set; } = SimNaoEnum.Nao;
    }
}