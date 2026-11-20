namespace CanindePwaShared.Models.Dto.AuthDto
{
    /// <summary>
    /// Resposta de POST /api/auth/validatepin (sucesso e falhas).
    /// Os nomes seguem o JSON do servidor (success, message, attemptsRemaining, newPinSent, expired, errors).
    /// </summary>
    public class ValidatePinResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        /// <summary>Tentativas que ainda restam para o PIN atual (null quando nao se aplica).</summary>
        public int? AttemptsRemaining { get; set; }

        /// <summary>true quando o limite de tentativas foi atingido e o servidor enviou um PIN novo por e-mail.</summary>
        public bool NewPinSent { get; set; }

        /// <summary>true quando o PIN informado expirou.</summary>
        public bool Expired { get; set; }

        /// <summary>Regras de senha violadas (PIN correto, mas senha nova recusada).</summary>
        public List<string> Errors { get; set; } = new();
    }
}
