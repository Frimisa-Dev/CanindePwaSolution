using CanindePwaShared.Enums;
using CanindePwaShared.Models.Auth;
using CanindePwaShared.Models.Dto.AuthDto;

namespace CanindeWebServer.Services
{
    public interface IAuthManager
    {
        Task<User?> ValidateUserByEmailAsync(string email, string password);
        Task<string> GenerateTokensAsync(User user);
        Task<string> MapTipoUsuarioToRole(UsuarioEnum tipoUsuario);
        Task AssignRoleToUserAsync(int userId, string roleName);
        Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash);
        Task SaveRefreshTokenAsync(int userId, string token, DateTime expiresAt, string createdByIp);
        Task RevokeRefreshTokenAsync(RefreshToken token, string? replacedByTokenHash = null);
        Task SaveEmailConfirmationTokenAsync(int userId, string token, DateTime expiresAt, string createdByIp);
        Task<RefreshToken?> GetEmailConfirmationTokenAsync(string tokenHash);
        Task SaveResetPasswordTokenAsync(int userId, string token, DateTime expiresAt, string createdByIp);

        // Recuperacao de senha: token vigente (nao revogado, nao expirado), data do ultimo pedido
        // (para o intervalo minimo entre pedidos) e revogacao em lote por finalidade.
        Task<RefreshToken?> GetResetPasswordTokenAsync(string tokenHash);
        Task<DateTime?> GetLastResetPasswordRequestAtAsync(int userId);
        Task RevokeUserTokensAsync(int userId, string purpose);
        Task<CadastroUsuarioResponseDto> CadastroUsuarioUserAsync(CadastroUsuarioDto dto);
    }
}