using System.ComponentModel.DataAnnotations;

namespace CanindePwaShared.Models.Dto.TableDto
{
    public class PessoaMotoristaRequestDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "ID_Pessoa é obrigatório.")]
        public int ID_Pessoa { get; set; }

        // Particularidade do Motorista: aponta para tblCdPessoaAgenciador (NOT NULL).
        [Range(1, int.MaxValue, ErrorMessage = "ID_PessoaAgenciador é obrigatório.")]
        public int ID_PessoaAgenciador { get; set; }
    }
}
