using FalcaoWebServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FalcaoWebServer.Services
{
    public class PinConfigurationService : IPinConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly FalcaoDbContext _db;

        public PinConfigurationService(IConfiguration configuration, FalcaoDbContext db)
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

            // 2. Default seguro
            return 10;
        }

        public async Task<int> GetPinMaxAttempts()
        {
            var registryValue = _configuration["PinMaxAttempts"];
            if (int.TryParse(registryValue, out var attempts) && attempts > 0)
                return attempts;

            return 3;
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