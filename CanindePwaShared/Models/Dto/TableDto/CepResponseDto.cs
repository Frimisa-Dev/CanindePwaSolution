using CanindePwaShared.Enums;

namespace CanindePwaShared.Models.Dto.TableDto
{
    public class CepResponseDto
    {
        public int ID_Cep { get; set; }
        public string Cep { get; set; } = string.Empty;

        public int ID_Logradouro { get; set; }
        public int ID_Bairro { get; set; }
        public int ID_Cidade { get; set; }
        public int ID_Estado { get; set; }

        // Descrições vindas da view uvwCep
        public string Logradouro { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;

        public TipoCepEnum TipoCep { get; set; }
        public EstatusEnum Estatus { get; set; }
        public ResultadoEnum Resultado { get; set; }
    }
}