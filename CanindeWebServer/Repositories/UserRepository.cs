using CanindeWebServer.Data;
using Microsoft.EntityFrameworkCore;
using CanindePwaShared.Models.Auth;

public class UserRepository : IUserRepository
{
    private readonly ProjetoDbContext _db;
    public UserRepository(ProjetoDbContext db) => _db = db;

    public async Task<User> GetByUsernameAsync(string username)
    {
        return await _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                              .SingleOrDefaultAsync(u => u.Username == username);
    }

    public async Task<Role[]> GetRolesForUserAsync(int userId)
    {
        return await _db.UserRoles.Where(ur => ur.UserId == userId)
                                 .Select(ur => ur.Role).ToArrayAsync();
    }

    public async Task AddRefreshTokenAsync(RefreshToken token)
    {
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();
    }

    public async Task<RefreshToken> GetRefreshTokenByHashAsync(string tokenHash)
    {
        return await _db.RefreshTokens.SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow);
    }

    public async Task RevokeRefreshTokenAsync(RefreshToken token, string? replacedByTokenHash = null)
    {
        token.RevokedAt = DateTime.UtcNow;
        token.ReplacedByTokenHash = replacedByTokenHash;
        _db.Update(token);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateUserAsync(User user)
    {
        _db.Update(user);
        await _db.SaveChangesAsync();
    }
}

