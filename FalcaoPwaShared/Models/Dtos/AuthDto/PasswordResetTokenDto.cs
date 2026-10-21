namespace FalcaoPwaShared.Models.Dtos.AuthDto
{
    public class PasswordResetTokenDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool Used { get; set; }
    }
}
