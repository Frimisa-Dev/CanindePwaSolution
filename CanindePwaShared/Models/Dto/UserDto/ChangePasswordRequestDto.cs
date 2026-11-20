/// <summary>
/// Alteração de senha
/// Uso: ChangePassword.razor → UsersController.ChangePassword()
/// </summary>
namespace CanindePwaShared.Models.Dto.UserDto
{
    public class ChangePasswordRequestDto
    {
        public required string CurrentPassword { get; set; }
        public required string NewPassword { get; set; }
    }
}