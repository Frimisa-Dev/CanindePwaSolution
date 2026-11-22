using System.ComponentModel.DataAnnotations;

namespace CanindePwaShared.Models.Dto.AuthDto
{
    /// <summary>
    /// Redefinicao de senha (passo 2: token do link do e-mail + senha nova)
    /// Uso: RedefinirSenha.razor -> AuthController.ResetPassword()
    /// O token identifica o usuario (nao ha mais e-mail no corpo): o servidor guarda so o hash do token.
    /// As regras da senha (tamanho, maiuscula, numero, simbolo, confirmacao) sao validadas no servidor.
    /// </summary>
    public class ResetPasswordDto
    {
        [Required]
        [StringLength(128)]
        public required string Token { get; set; } // Token do link enviado por e-mail (256 bits, Base64Url)

        [StringLength(256)] // limite so para evitar abuso; as regras reais ficam no servidor
        public required string NewPassword { get; set; }

        [StringLength(256)]
        public required string ConfirmPassword { get; set; }
    }
}
