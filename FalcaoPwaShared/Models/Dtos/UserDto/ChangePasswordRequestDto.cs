/// <summary>
/// Alteração de senha
/// Uso: ChangePassword.razor → UsersController.ChangePassword()
/// </summary>
namespace FalcaoPwaShared.Models.Dtos.UserDto
{
    public class ChangePasswordRequestDto
    {
        public required string CurrentPassword { get; set; }
        public required string NewPassword { get; set; }
    }
}