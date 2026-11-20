namespace CanindeWebServer.Services.Utils
{
    public static class UtilizarEnum
    {
        public static T ParseOrDefault<T>(string? value, T defaultValue) where T : struct, Enum
        {
            return !string.IsNullOrWhiteSpace(value) && Enum.TryParse<T>(value, true, out var parsed) ? parsed : defaultValue;
        }
    }
}