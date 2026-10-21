using System.Net;

namespace FalcaoPwaClient.Handlers
{
    /// <summary>
    /// Handler de retry inteligente - Distingue erros de negócio vs técnicos
    /// Nivel Bancário: Lógica sofisticada de retry baseada em contexto
    /// </summary>
    public class WasmRetryHandler : DelegatingHandler
    {
        private const int MaxRetryCount = 3;
        private const int BaseDelayMs = 1000;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage? response = null;
            var lastException = (Exception?)null;

            for (int attempt = 1; attempt <= MaxRetryCount; attempt++)
            {
                try
                {
                    response = await base.SendAsync(request, cancellationToken);

                    // ✅ SUCESSO - retorna imediatamente
                    if (response.IsSuccessStatusCode)
                        return response;

                    // ✅ ERROS QUE NÃO DEVEM SER RETRIED (INTELIGÊNCIA Handlers/)
                    if (IsBusinessError(response.StatusCode))
                    {
                        return response; // Não retry em erros de negócio
                    }

                    // ✅ ERROS TRANSIENTES - FAZ RETRY (MELHORIA Services/)
                    if (ShouldRetry(response.StatusCode) && attempt < MaxRetryCount)
                    {
                        await ExponentialBackoff(attempt, cancellationToken);
                        continue;
                    }

                    return response;
                }
                catch (HttpRequestException ex) when (attempt < MaxRetryCount)
                {
                    lastException = ex;
                    await ExponentialBackoff(attempt, cancellationToken);
                }
                catch (TaskCanceledException ex) when (attempt < MaxRetryCount)
                {
                    lastException = ex;
                    await ExponentialBackoff(attempt, cancellationToken);
                }
            }

            // ✅ FALLBACK ELEGANTE
            return response ?? CreateFallbackResponse(lastException);
        }

        /// <summary>
        /// Erros de negócio - NUNCA fazer retry (INTELIGÊNCIA Handlers/)
        /// </summary>
        private static bool IsBusinessError(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.BadRequest ||
                   statusCode == HttpStatusCode.Unauthorized ||
                   statusCode == HttpStatusCode.NotFound ||
                   statusCode == HttpStatusCode.Forbidden ||
                   statusCode == HttpStatusCode.Conflict;
        }

        /// <summary>
        /// Erros técnicos transient - FAZER RETRY (MELHORIA Services/)
        /// </summary>
        private static bool ShouldRetry(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.RequestTimeout ||
                   statusCode == HttpStatusCode.TooManyRequests ||
                   statusCode == HttpStatusCode.InternalServerError ||
                   statusCode == HttpStatusCode.BadGateway ||
                   statusCode == HttpStatusCode.ServiceUnavailable ||
                   statusCode == HttpStatusCode.GatewayTimeout;
        }

        private static async Task ExponentialBackoff(int attempt, CancellationToken cancellationToken)
        {
            var delay = TimeSpan.FromMilliseconds(BaseDelayMs * Math.Pow(2, attempt - 1));
            await Task.Delay(delay, cancellationToken);
        }

        private static HttpResponseMessage CreateFallbackResponse(Exception? ex)
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = $"Service unavailable after retries: {ex?.Message}"
            };
        }
    }
}