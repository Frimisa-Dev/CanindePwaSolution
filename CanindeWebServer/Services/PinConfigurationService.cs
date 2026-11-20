using CanindeWebServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CanindeWebServer.Services
{
    public class PinConfigurationService : IPinConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ProjetoDbContext _db;

        public PinConfigurationService(IConfiguration configuration, ProjetoDbContext db)
        {
            _configuration = configuration;
            _db = db;
        }

        public async Task<int> GetPinExpirationMinutes()
        {
            // 1. Tenta Registry (Configuration)
            var registryValue = _configuration["PinExpirationMinutes"];
            if (int.TryParse(registryValue, out var minutes) && minutes > 0)
                return minutes;

            // 2. Default seguro: PIN válido por 30 minutos (regra de homologação de usuário)
            return 30;
        }

        public async Task<int> GetPinMaxAttempts()
        {
            var registryValue = _configuration["PinMaxAttempts"];
            if (int.TryParse(registryValue, out var attempts) && attempts > 0)
                return attempts;

            // Padrão alinhado ao AuthController (MaxTentativasPin = 5): 5 tentativas de PIN
            return 5;
        }

        public async Task<int> GetPinResendCooldownMinutes()
        {
            var registryValue = _configuration["PinResendCooldownMinutes"];
            if (int.TryParse(registryValue, out var minutes) && minutes >= 0)
                return minutes;

            return 5;
        }

        public async Task<DateTime> CalculatePinExpiration()
        {
            var minutes = await GetPinExpirationMinutes();
            return DateTime.UtcNow.AddMinutes(minutes);
        }
    }
}