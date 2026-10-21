using System.Net;
using System.Collections.Concurrent;

namespace FalcaoPwaClient.Handlers
{
    /// <summary>
    /// Circuit Breaker avançado - Proteção máxima com timeout integrado
    /// Nivel Bancário: Estado completo + proteção concorrente
    /// </summary>
    public class WasmCircuitBreakerHandler : DelegatingHandler
    {
        private readonly ConcurrentDictionary<string, CircuitState> _circuits = new();
        private readonly TimeSpan _breakDuration = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
        private const int FailureThreshold = 3;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var circuitKey = GetCircuitKey(request);

            // ✅ VERIFICA SE CIRCUITO ESTÁ ABERTO (ROBUSTEZ Handlers/)
            if (IsCircuitOpen(circuitKey))
            {
                return CreateCircuitBrokenResponse();
            }

            try
            {
                // ✅ TIMEOUT PROTECTION INTEGRADO (MELHORIA Handlers/)
                using var timeoutCts = new CancellationTokenSource(_timeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var response = await base.SendAsync(request, linkedCts.Token);

                if (response.IsSuccessStatusCode)
                {
                    // ✅ SUCESSO - reseta circuito
                    ResetCircuit(circuitKey);
                    return response;
                }
                else
                {
                    // ✅ FALHA HTTP - registra falha
                    RecordFailure(circuitKey);
                    return response;
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // ✅ TIMEOUT - registra falha
                RecordFailure(circuitKey);
                return new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                {
                    ReasonPhrase = "Request timeout - circuit breaker active"
                };
            }
            catch (Exception ex)
            {
                // ✅ OUTRAS EXCEÇÕES - registra falha
                RecordFailure(circuitKey);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = $"Circuit breaker error: {ex.Message}"
                };
            }
        }

        private bool IsCircuitOpen(string circuitKey)
        {
            if (_circuits.TryGetValue(circuitKey, out var state))
            {
                if (state.State == CircuitStateType.Open)
                {
                    // ✅ VERIFICA SE DEVE TENTAR RECUPERAR (HALF-OPEN)
                    if (DateTime.UtcNow - state.LastStateChange > _breakDuration)
                    {
                        state.State = CircuitStateType.HalfOpen;
                        state.FailureCount = 0;
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        private void RecordFailure(string circuitKey)
        {
            var state = _circuits.AddOrUpdate(circuitKey,
                new CircuitState { FailureCount = 1, State = CircuitStateType.Closed },
                (key, existing) =>
                {
                    existing.FailureCount++;
                    if (existing.FailureCount >= FailureThreshold)
                    {
                        existing.State = CircuitStateType.Open;
                        existing.LastStateChange = DateTime.UtcNow;
                    }
                    return existing;
                });
        }

        private void ResetCircuit(string circuitKey)
        {
            _circuits.AddOrUpdate(circuitKey,
                new CircuitState { FailureCount = 0, State = CircuitStateType.Closed },
                (key, existing) => new CircuitState { FailureCount = 0, State = CircuitStateType.Closed });
        }

        private static string GetCircuitKey(HttpRequestMessage request)
        {
            return $"{request.Method}_{request.RequestUri?.Host}";
        }

        private static HttpResponseMessage CreateCircuitBrokenResponse()
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = "Circuit breaker open - service temporarily unavailable"
            };
        }

        private class CircuitState
        {
            public int FailureCount { get; set; }
            public CircuitStateType State { get; set; }
            public DateTime LastStateChange { get; set; } = DateTime.UtcNow;
        }

        private enum CircuitStateType
        {
            Closed,
            HalfOpen,
            Open
        }
    }
}
