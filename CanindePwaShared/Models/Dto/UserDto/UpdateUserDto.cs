namespace CanindePwaShared.Models.Dto.UserDto
{
    public class UpdateUserDto
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; } = true;
    }

}
