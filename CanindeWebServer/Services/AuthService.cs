using CanindePwaShared.Enums;
using CanindePwaShared.Models.Auth;
using CanindePwaShared.Models.Dto.AuthDto;
using CanindeWebServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace CanindeWebServer.Services
{
    public class AuthManager : IAuthManager
    {
        private readonly ProjetoDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthManager> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITempPasswordService _tempPasswordService;

        public AuthManager(
            ProjetoDbContext db,
            IPasswordHasher passwordHasher,
            IConfiguration configuration,
            ILogger<AuthManager> logger,
            IHttpContextAccessor httpContextAccessor,
            ITempPasswordService tempPasswordService)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _tempPasswordService = tempPasswordService;
        }

        public async Task<User?> ValidateUserByEmailAsync(string email, string password)
        {
            _logger.LogInformation("[AUTH] Validando: {Email}", email);

            var user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u =>
                    u.Email == email &&
                    !u.IsLocked);

            if (user == null)
            {
                _logger.LogWarning("[AUTH] Usuario nao encontrado: {Email}", email);
                return null;
            }

            // Verificar senha com BCrypt
            var isValid = _passwordHasher.VerifyPassword(password, user.PasswordHash);

            if (isValid)
            {
                _logger.LogInformation("[AUTH] Login valido: {Email}", email);

                // Atualizar hash se necessario
                if (_passwordHasher.NeedsRehash(user.PasswordHash))
                {
                    user.PasswordHash = _passwordHasher.HashPassword(password);
                    _logger.LogInformation("[AUTH] Hash atualizado para {Email}", email);
                }

                user.LastLoginAt = DateTime.UtcNow;
                _db.Users.Update(user);
                await _db.SaveChangesAsync();

                return user;
            }
            else
            {
                _logger.LogWarning("[AUTH] Senha invalida para: {Email}", email);
                return null;
            }
        }

        public Task<string> MapTipoUsuarioToRole(UsuarioEnum tipoUsuario)
        {
            var role = tipoUsuario switch
            {
                UsuarioEnum.Ceo => "Ceo",
                UsuarioEnum.Administrador => "Administrador",
                UsuarioEnum.Financeiro => "Financeiro",
                UsuarioEnum.Operador => "Operador",
                _ => "Operador"
            };

            return Task.FromResult(role);
        }

        public async Task AssignRoleToUserAsync(int userId, string roleName)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
            {
                role = new Role { Name = roleName, Description = $"Role para {roleName}" };
                _db.Roles.Add(role);
                await _db.SaveChangesAsync();
            }

            var existing = await _db.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);

            if (!existing)
            {
                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = role.Id,
                    User = null!,
                    Role = null!
                };
                _db.UserRoles.Add(userRole);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<string> GenerateTokensAsync(User user)
        {
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var clientIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
                ?? "0.0.0.0";

            await SaveRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(7), clientIp);

            return (refreshToken);
        }

        public async Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash)
        {
            return await _db.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && rt.RevokedAt == null);
        }

        public async Task SaveRefreshTokenAsync(int userId, string token, DateTime expiresAt, string createdByIp)
        {
            var tokenHash = ToSha256(token);

            var refreshToken = new RefreshToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = createdByIp,
                Purpose = "Authentication",
                User = null!
            };

            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();
        }

        public async Task RevokeRefreshTokenAsync(RefreshToken token, string? replacedByTokenHash = null)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.ReplacedByTokenHash = replacedByTokenHash;
            _db.RefreshTokens.Update(token);
            await _db.SaveChangesAsync();
        }

        public async Task SaveEmailConfirmationTokenAsync(int userId, string token, DateTime expiresAt, string createdByIp)
        {
            var tokenHash = ToSha256(token);

            var refreshToken = new RefreshToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = createdByIp,
                Purpose = "EmailConfirmation",
                User = null!
            };

            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();
        }

        public async Task<RefreshToken?> GetEmailConfirmationTokenAsync(string tokenHash)
        {
            return await _db.RefreshTokens
                .FirstOrDefaultAsync(rt =>
                    rt.TokenHash == tokenHash &&
                    rt.Purpose == "EmailConfirmation" &&
                    rt.RevokedAt == null);
        }

        public async Task SaveResetPasswordTokenAsync(int userId, string token, DateTime expiresAt, string createdByIp)
        {
            var tokenHash = ToSha256(token);

            var refreshToken = new RefreshToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = createdByIp,
                Purpose = "PasswordReset",
                User = null!
            };

            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();
        }

        // ===============================================================
        // RECUPERACAO DE SENHA
        // ===============================================================

        /// <summary>
        /// Token de redefinicao VIGENTE: finalidade PasswordReset, nao revogado e nao expirado.
        /// Devolve a entidade rastreada (o controller marca RevokedAt e grava junto com a senha nova).
        /// </summary>
        public async Task<RefreshToken?> GetResetPasswordTokenAsync(string tokenHash)
        {
            var agora = DateTime.UtcNow;

            return await _db.RefreshTokens
                .FirstOrDefaultAsync(rt =>
                    rt.TokenHash == tokenHash &&
                    rt.Purpose == "PasswordReset" &&
                    rt.RevokedAt == null &&
                    rt.ExpiresAt > agora);
        }

        /// <summary>
        /// Quando o usuario pediu a redefinicao pela ultima vez (conta tokens revogados tambem),
        /// para impor o intervalo minimo entre pedidos.
        /// </summary>
        public async Task<DateTime?> GetLastResetPasswordRequestAtAsync(int userId)
        {
            return await _db.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.Purpose == "PasswordReset")
                .OrderByDescending(rt => rt.CreatedAt)
                .Select(rt => (DateTime?)rt.CreatedAt)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Revoga todos os tokens ainda ativos do usuario de uma finalidade
        /// ("PasswordReset" = links anteriores; "Authentication" = sessoes de refresh).
        /// </summary>
        public async Task RevokeUserTokensAsync(int userId, string purpose)
        {
            var tokens = await _db.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.Purpose == purpose && rt.RevokedAt == null)
                .ToListAsync();

            if (tokens.Count == 0)
                return;

            var agora = DateTime.UtcNow;
            foreach (var token in tokens)
                token.RevokedAt = agora;

            await _db.SaveChangesAsync();
        }

        private static string ToSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public async Task<CadastroUsuarioResponseDto> CadastroUsuarioUserAsync(CadastroUsuarioDto dto)
        {
            // VALIDACOES BASICAS
            if (dto.Password != dto.ConfirmPassword)
            {
                return new CadastroUsuarioResponseDto
                {
                    Success = false,
                    Message = "As senhas nao conferem."
                };
            }

            // VERIFICAR SE EMAIL JA EXISTE
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (existingUser != null)
            {
                return new CadastroUsuarioResponseDto
                {
                    Success = false,
                    Message = "E-mail ja cadastrado."
                };
            }

            try
            {
                // CRIAR USUARIO COM BCrypt
                var user = new User
                {
                    Email = dto.Email,
                    PasswordHash = _passwordHasher.HashPassword(dto.Password),
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow,
                    Username = dto.Username ?? dto.Email.Split('@')[0],
                    TipoUsuario = dto.TipoUsuario.ToString()
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                // SALVAR SENHA TEMPORARIA
                await _tempPasswordService.StoreTempPasswordAsync(user.Id, dto.Password);

                // ATRIBUIR ROLE
                var roleName = await MapTipoUsuarioToRole(dto.TipoUsuario);
                await AssignRoleToUserAsync(user.Id, roleName);

                // GERAR PIN DE CONFIRMACAO
                var pin = new Random().Next(100000, 999999).ToString();
                var expiresAt = DateTime.UtcNow.AddMinutes(10);

                _db.EmailPins.Add(new EmailPin
                {
                    UserId = user.Id,
                    Code = pin,
                    ExpiresAt = expiresAt
                });

                await _db.SaveChangesAsync();

                _logger.LogInformation("[CADASTRARUSUARIO] Usuario criado: {Email} (ID: {Id})", user.Email, user.Id);

                return new CadastroUsuarioResponseDto
                {
                    Success = true,
                    Message = "Usuario criado com sucesso! Verifique seu e-mail.",
                    UserId = user.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CADASTRARUSUARIO] Erro ao cadastrar usuario: {Email}", dto.Email);

                return new CadastroUsuarioResponseDto
                {
                    Success = false,
                    Message = "Erro interno ao cadastrar usuario."
                };
            }
        }
    }
}
