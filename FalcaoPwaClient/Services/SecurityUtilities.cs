using System.Security.Cryptography;

namespace FalcaoPwaClient.Services
{
    public static class SecurityUtilities
    {
        private static string? _clientId;

        private static string GenerateClientId()
        {
            return Guid.NewGuid().ToString();
        }

        public static string GetClientIdentifier()
        {
            if (string.IsNullOrEmpty(_clientId))
            {
                using var sha = SHA256.Create();
                var machineId = $"{Environment.MachineName}_{Environment.UserName}_{Guid.NewGuid()}";
                var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(machineId));
                _clientId = Convert.ToBase64String(hash)[..16]; // Primeiros 16 chars
            }

            return _clientId;
        }

        public static string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input.Trim()
                        .Replace("<", "&lt;")
                        .Replace(">", "&gt;")
                        .Replace("\"", "&quot;")
                        .Replace("'", "&#x27;");
        }
    }
}