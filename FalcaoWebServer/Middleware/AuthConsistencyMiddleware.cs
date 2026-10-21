using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FalcaoWebServer.Middleware
{
    public class AuthConsistencyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthConsistencyMiddleware> _logger;

        public AuthConsistencyMiddleware(
            RequestDelegate next,
            ILogger<AuthConsistencyMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/api/auth"))
            {
                var hasCookieAuth = context.Request.Cookies.ContainsKey(".AspNetCore.Cookies"); // Cookie de sessão
                var hasFalcaoToken = context.Request.Cookies.ContainsKey("Falcao.Auth");
            }

            var endpoint = context.GetEndpoint();

            // Só validar endpoints que requerem autenticação
            if (endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAuthorizeData>() != null)
            {
                var hasCookie = context.Request.Cookies.ContainsKey("Falcao.Auth");
                var hasBearer = context.Request.Headers.ContainsKey("Authorization");

                // 🚫 CASO INVÁLIDO 1: Ambos presentes
                if (hasCookie && hasBearer)
                {
                    _logger.LogWarning(
                        "Autenticação ambígua de {RemoteIp}. Request contém Cookie e Bearer Token.",
                        context.Connection.RemoteIpAddress);

                    context.Response.StatusCode = 400; // Bad Request
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        error = "AuthConflict",
                        message = "Use apenas um método de autenticação por request: Cookie OU Bearer Token.",
                        timestamp = DateTime.UtcNow
                    }));
                    return;
                }

                // ✅ LOG de modo de autenticação detectado
                if (hasCookie)
                {
                    _logger.LogDebug("Request autenticado via Cookie");
                }
                else if (hasBearer)
                {
                    _logger.LogDebug("Request autenticado via Bearer Token");
                }
            }

            await _next(context);
        }
    }
}

