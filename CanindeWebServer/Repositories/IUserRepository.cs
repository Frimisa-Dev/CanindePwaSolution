using CanindePwaShared.Models.Auth;
using System.Threading.Tasks;

public interface IUserRepository
{
    Task<User> GetByUsernameAsync(string username);
    Task<Role[]> GetRolesForUserAsync(int userId);
    Task AddRefreshTokenAsync(RefreshToken token);
    Task<RefreshToken> GetRefreshTokenByHashAsync(string tokenHash);
    Task RevokeRefreshTokenAsync(RefreshToken token, string? replacedByTokenHash = null);
    Task UpdateUserAsync(User user);
}
