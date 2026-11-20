using Microsoft.Extensions.Logging;

namespace CanindeWebServer.Services
{
    public class BCryptPasswordHasher : IPasswordHasher
    {
        private readonly ILogger<BCryptPasswordHasher> _logger;
        private const int WorkFactor = 12;

        public BCryptPasswordHasher(ILogger<BCryptPasswordHasher> logger)
        {
            _logger = logger;



            _logger.LogInformation("BCryptPasswordHasher instanciado");

        }

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("A senha nao pode ser vazia.");

            try
            {
                // Metodo padrao do BCrypt - compativel com BCrypt.Net-Next
                return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar hash de senha.");
                throw;
            }
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
            {
                _logger.LogWarning("Tentativa de verificacao com senha ou hash vazio.");
                return false;
            }

            try
            {
                // Metodo padrao do BCrypt - compativel com BCrypt.Net-Next
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar senha.");
                return false;
            }
        }

        public bool NeedsRehash(string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
                return true;

            try
            {
                // Metodo padrao para verificar necessidade de rehash
                return BCrypt.Net.BCrypt.PasswordNeedsRehash(hashedPassword, WorkFactor);
            }
            catch
            {
                // Se falhar, assume que precisa ser refeito
                return true;
            }
        }
    }
}