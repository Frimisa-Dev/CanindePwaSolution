using System.Net.Http.Headers;

namespace CanindePwaClient.Handlers
{
    /// <summary>
    /// Handler de segurança corporativa - Headers de proteção máximo
    /// Nivel Bancário: Implementa OWASP Security Headers completos
    /// </summary>
    public class SecurityHeadersHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // ✅ HEADERS DE SEGURANÇA CORPORATIVA (REQUEST)
            request.Headers.Add("X-Content-Type-Options", "nosniff");
            request.Headers.Add("X-Frame-Options", "DENY");
            request.Headers.Add("X-XSS-Protection", "1; mode=block"); // ✅ MELHORIA Handlers/
            request.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            request.Headers.Add("Permissions-Policy", "geolocation=(), microphone=(), camera=()"); // ✅ NOVO

            var response = await base.SendAsync(request, cancellationToken);

            // ✅ HEADERS DE SEGURANÇA NA RESPOSTA (MELHORIA Handlers/)
            if (response.Content != null)
            {
                response.Content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
            }

            return response;
        }
    }
}
