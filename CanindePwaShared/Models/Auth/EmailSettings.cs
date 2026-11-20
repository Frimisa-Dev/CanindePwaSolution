namespace CanindePwaShared.Models.Auth
{
    // Configuracoes de Email (para DI)
    public class EmailSettings
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 587;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string From { get; set; } = "no-reply@curio.com";
        public bool UseSsl { get; set; } = false;
        public bool UseStartTls { get; set; } = true;
    }
}
