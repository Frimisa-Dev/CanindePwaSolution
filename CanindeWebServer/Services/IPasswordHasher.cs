namespace CanindeWebServer.Services
{
    /// <summary>
    /// Interface para hashing e verificacao de senhas
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Cria hash BCrypt de uma senha
        /// </summary>
        string HashPassword(string password);

        /// <summary>
        /// Verifica se uma senha corresponde ao hash
        /// </summary>
        bool VerifyPassword(string password, string hashedPassword);

        /// <summary>
        /// Verifica se o hash precisa ser atualizado (work factor mais forte)
        /// </summary>
        bool NeedsRehash(string hashedPassword);
    }
}