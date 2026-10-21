using FalcaoPwaShared.Enums;
using FalcaoPwaShared.Models.Auth;
using FalcaoPwaShared.Models.Dtos.AuthDto;
using FalcaoWebServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.IO.Pipelines;

namespace FalcaoWebServer.Services
{
    /// <summary>
    /// [INFRAESTRUTURA] Servico especializado para bootstrap inicial do sistema.
    /// ?? NAO usar para casos de uso de negocio. Use IAuthManager para isso.
    /// 
    /// MOTIVACAO ARQUITETURAL:
    /// =======================
    /// O sistema Falcao possui DOIS fluxos distintos de criacao de usuario:
    /// 
    /// 1. BOOTSTRAP INICIAL (Seed)
    ///    - Contexto: Sistema vazio, primeira execucao
    ///    - Requisitos: Criar usuario mestre SEM PIN, SEM confirmacao, ATIVO imediatamente
    ///    - Responsavel: Program.cs (infraestrutura)
    ///    
    /// 2. FLUXO NORMAL DE NEGOCIO
    ///    - Contexto: Sistema operacional, admin autenticado
    ///    - Requisitos: PIN, confirmacao de e-mail, auditoria completa
    ///    - Responsavel: AuthController (dominio)
    /// </summary>
    /// <summary>
    /// Implementacao do servico de bootstrap do sistema.
    /// DIFERENCAS DO AuthManager:
    /// 1. Usuario criado como ATIVO (IsActive = true)
    /// 2. Sem PIN de confirmacao
    /// 3. Sem e-mail de verificacao
    /// 4. Logs focados em infraestrutura, nao negocio
    /// </summary>
    public class AdminSeedService : IAdminSeedService
    {
        private readonly FalcaoDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AdminSeedService> _logger;

        public AdminSeedService(
            FalcaoDbContext db,
            IPasswordHasher passwordHasher,
            ILogger<AdminSeedService> logger)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<SeedResultDto> SeedAdministrativeUserAsync(SeedAdminDto dto)
        {
            try
            {
                // 1. Verificar se ja existe
                var existing = await _db.Users.AnyAsync(u => u.Email == dto.Email);
                if (existing)
                {
                    return new SeedResultDto
                    {
                        Success = false,
                        Message = $"Usuario {dto.Email} ja existe no sistema."
                    };
                }

                // 2. Criar hash da senha (mesmo hasher do AuthManager, mas contexto diferente)
                var passwordHash = _passwordHasher.HashPassword(dto.Password);

                // 3. Criar usuario ADMIN ATIVO (bypass do fluxo normal)
                var user = new User
                {
                    Username = dto.Username,
                    Email = dto.Email,
                    PasswordHash = passwordHash,
                    IsActive = true,  // ?? DIFERENCA CRITICA: Ativo imediatamente
                    IsLocked = false,
                    TipoUsuario = dto.TipoUsuario.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = null
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                var roleName = dto.TipoUsuario.ToString();

                var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role == null)
                {
                    role = new Role
                    {
                        Name = roleName,
                        Description = $"Role bootstrap para {roleName}"
                    };

                    _db.Roles.Add(role);
                    await _db.SaveChangesAsync();
                }

                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,

                    // required pelo model - inicializados explicitamente
                    User = null!,
                    Role = null!
                };

                _db.UserRoles.Add(userRole);
                await _db.SaveChangesAsync();

                return new SeedResultDto
                {
                    Success = true,
                    Message = "Usuario administrativo criado com sucesso (bypass de fluxo normal justificado).",
                    UserId = user.Id,
                    GeneratedHashPrefix = passwordHash.Substring(0, 20)
                };
            }
            catch (Exception ex)
            {
                return new SeedResultDto
                {
                    Success = false,
                    Message = $"Erro tecnico no bootstrap: {ex.Message}"
                };
            }
        }
    }
}
