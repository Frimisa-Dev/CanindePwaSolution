namespace FalcaoWebServer.Services
{
    public interface IPinConfigurationService
    {
        Task<int> GetPinExpirationMinutes();
        Task<int> GetPinMaxAttempts();
        Task<int> GetPinResendCooldownMinutes();
        Task<DateTime> CalculatePinExpiration();
    }
}
