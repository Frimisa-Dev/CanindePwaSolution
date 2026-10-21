public interface ITempPasswordService
{
    Task<string> StoreTempPasswordAsync(int userId, string passwordPlain);
    Task<string?> GetAndInvalidateTempPasswordAsync(int userId);
    Task CleanupExpiredTempPasswordsAsync();
}

