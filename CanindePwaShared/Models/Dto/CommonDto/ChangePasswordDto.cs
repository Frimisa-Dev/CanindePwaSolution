/// <summary>
/// Alteração de senha do usuário
/// Uso: ChangePassword.razor → UsersController.ChangePassword()
/// </summary>
namespace CanindePwaShared.Models.Dto.CommonDto
{
    public class ChangePasswordDto
    {
        public required string CurrentPassword { get; set; }
        public required string NewPassword { get; set; }
    }
}