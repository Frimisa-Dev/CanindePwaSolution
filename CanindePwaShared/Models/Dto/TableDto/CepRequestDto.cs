using CanindePwaShared.Enums;
using System.ComponentModel.DataAnnotations;

namespace CanindePwaShared.Models.Dto.TableDto
{
    public class CepRequestDto
    {
        [Required]
        [RegularExpression(@"^\d{5}-\d{3}$", ErrorMessage = "CEP deve estar no formato 00000-000")]
        public required string Cep { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ID_Logradouro é obrigatório.")]
        public int ID_Logradouro { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ID_Bairro é obrigatório.")]
        public int ID_Bairro { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ID_Cidade é obrigatório.")]
        public int ID_Cidade { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ID_Estado é obrigatório.")]
        public int ID_Estado { get; set; }

        // Padrão seguro: quem não informar o tipo grava como Provisorio.
        public TipoCepEnum TipoCep { get; set; } = TipoCepEnum.Provisorio;

        public EstatusEnum Estatus { get; set; } = EstatusEnum.Ativo;

        public ResultadoEnum Resultado { get; set; } = ResultadoEnum.OK;
    }
}