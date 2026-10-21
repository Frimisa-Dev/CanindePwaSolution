using FalcaoPwaShared.Enums;
using System.ComponentModel.DataAnnotations;

namespace FalcaoPwaShared.Models.Dtos.AuthDto
{
    /// <summary>
    /// Registro de novo usuario
    /// Uso: Cadastrousuario.razor  AuthController.Cadastrousuario()
    /// </summary>
    public class CadastroUsuarioDto
    {
        [Required(ErrorMessage = "Nome de usuário é obrigatório")]
        [MinLength(3, ErrorMessage = "Nome deve ter pelo menos 3 caracteres")]
        public required string Username { get; set; }

        [Required(ErrorMessage = "E-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Senha é obrigatória")]
        [MinLength(6, ErrorMessage = "Senha deve ter pelo menos 6 caracteres")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Confirmação de senha é obrigatória")]
        [Compare(nameof(Password), ErrorMessage = "As senhas não conferem")]
        public required string ConfirmPassword { get; set; }

        public required UsuarioEnum TipoUsuario { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
    }
}