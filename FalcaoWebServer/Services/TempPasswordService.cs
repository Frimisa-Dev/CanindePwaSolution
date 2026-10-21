using FalcaoPwaShared.Models.Auth;
using FalcaoWebServer.Data;
using Microsoft.EntityFrameworkCore;

public class TempPasswordService : ITempPasswordService
{
    private readonly FalcaoDbContext _db;

    public TempPasswordService(FalcaoDbContext db)
    {
        _db = db;
    }

    public async Task<string> StoreTempPasswordAsync(int userId, string passwordPlain)
    {
        // Invalida senhas temporárias anteriores
        var oldPasswords = await _db.TempPasswords
            .Where(tp => tp.UserId == userId && !tp.Used)
            .ToListAsync();

        foreach (var old in oldPasswords)
        {
            old.Used = true;
        }

        // Cria nova senha temporária
        var tempPassword = new TempPassword
        {
            UserId = userId,
            PasswordPlain = passwordPlain,
            ExpiresAt = DateTime.UtcNow.AddHours(24), // Expira em 24h
            Used = false
        };

        _db.TempPasswords.Add(tempPassword);
        await _db.SaveChangesAsync();

        return passwordPlain;
    }

    public async Task<string?> GetAndInvalidateTempPasswordAsync(int userId)
    {
        var tempPassword = await _db.TempPasswords
            .Where(tp => tp.UserId == userId && !tp.Used && tp.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(tp => tp.CreatedAt)
            .FirstOrDefaultAsync();

        if (tempPassword == null)
            return null;

        // Marca como usado
        tempPassword.Used = true;
        await _db.SaveChangesAsync();

        return tempPassword.PasswordPlain;
    }

    public async Task CleanupExpiredTempPasswordsAsync()
    {
        var expiredPasswords = await _db.TempPasswords
            .Where(tp => tp.ExpiresAt <= DateTime.UtcNow || tp.Used)
            .ToListAsync();

        _db.TempPasswords.RemoveRange(expiredPasswords);
        await _db.SaveChangesAsync();
    }
}