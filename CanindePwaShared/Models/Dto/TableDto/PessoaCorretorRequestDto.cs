using System.ComponentModel.DataAnnotations;

namespace CanindePwaShared.Models.Dto.TableDto
{
    public class PessoaCorretorRequestDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "ID_Pessoa é obrigatório.")]
        public int ID_Pessoa { get; set; }
    }
}
