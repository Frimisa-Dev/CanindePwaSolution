namespace FalcaoPwaShared.Models.Dtos.AuthDto
{
    public class PinStatusResponseDto
    {
        public bool HasPendingPin { get; set; }
        public bool IsExpired { get; set; }
        public int? MinutesRemaining { get; set; }
        public string? UserEmail { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
