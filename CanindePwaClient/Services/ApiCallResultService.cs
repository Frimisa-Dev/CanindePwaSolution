namespace CanindePwaClient.Services
{
    /// <summary>
    /// Resultado de uma chamada HTTP que preserva o status e o corpo da resposta, inclusive em erro.
    /// Criado para telas que precisam dos detalhes devolvidos pelo servidor em 400/401
    /// (o PostAsync comum descarta tudo e devolve default).
    /// </summary>
    public class ApiCallResultService<T>
    {
        /// <summary>true quando o servidor respondeu 2xx.</summary>
        public bool IsSuccess { get; init; }

        /// <summary>Codigo HTTP; 0 quando nao houve resposta (rede, timeout, circuit breaker).</summary>
        public int StatusCode { get; init; }

        /// <summary>Corpo JSON desserializado (tambem em erro); null se vazio ou nao for JSON.</summary>
        public T? Body { get; init; }
    }
}
