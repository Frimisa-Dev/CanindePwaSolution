namespace FalcaoPwaShared.Models.Uvw
{
    public class UvwParametro
    {
        public int ID_Parametro { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string IdiomaData { get; set; } = string.Empty;
        public int MinimoDiasLimpezaBancoDados { get; set; }
        public string Versao { get; set; } = string.Empty;
    }
}
