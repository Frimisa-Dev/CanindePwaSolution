namespace FalcaoPwaShared.Models.Dtos.AuthDto
{
    public class SeedResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string? GeneratedHashPrefix { get; set; }
    }
}
