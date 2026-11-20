using CanindePwaShared.Enums;

namespace CanindePwaShared.Models.Tbl
{
    public class TblCep
    {
        public int ID_Cep { get; set; }
        public string Cep { get; set; } = string.Empty;
        public int ID_Logradouro { get; set; } = 0;
        public int ID_Bairro { get; set; } = 0;
        public int ID_Cidade { get; set; } = 0;
        public int ID_Estado { get; set; } = 0;
        public TipoCepEnum TipoCep { get; set; } 
        public EstatusEnum Estatus { get; set; }
        public ResultadoEnum Resultado { get; set; }
    }
}