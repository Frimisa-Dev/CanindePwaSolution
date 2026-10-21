namespace FalcaoPwaShared.Models.Dtos.AuthDto
{
    public class PendingPinDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired { get; set; }
        public int MinutesRemaining { get; set; }
    }
}