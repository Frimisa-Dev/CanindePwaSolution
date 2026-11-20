using CanindePwaShared.Enums;
using CanindePwaShared.Models.Auth;
using CanindePwaShared.Models.Dto.AuthDto;
using CanindeWebServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.IO.Pipelines;

namespace CanindeWebServer.Services
{
    /// <summary>
    /// [INFRAESTRUTURA] Servico especializado para bootstrap inicial do sistema.
    /// ?? NAO usar para casos de uso de negocio. Use IAuthManager para isso.
    /// 
    /// MOTIVACAO ARQUITETURAL:
    /// =======================
    /// O sistema Caninde possui DOIS fluxos distintos de criacao de usuario:
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
    ///
    /// REGRAS DA SEED (validadas AQUI, alem das validacoes do Program.cs):
    /// - So executa com a tabela Users VAZIA (primeira execucao). Com qualquer usuario ja
    ///   cadastrado, a seed e ignorada e nada e gravado.
    /// - Username e e-mail obrigatorios; senha com no minimo 8 caracteres.
    /// - Login padrao de instalacao (definido pelo Start-Dev.ps1 no Registry):
    ///   nome_do_projeto@instalacao.com / Nome_do_projeto@123  (ex.: caninde@instalacao.com / Caninde@123).
    ///   Essa senha e so para o primeiro acesso; o administrador deve troca-la em seguida (changepassword).
    /// </summary>
    public class AdminSeedService : IAdminSeedService
    {
        private readonly ProjetoDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AdminSeedService> _logger;

        /// <summary>Tamanho minimo da senha inicial do administrador (mesmo minimo das senhas do AuthController).</summary>
        private const int TamanhoMinimoSenha = 8;

        public AdminSeedService(
            ProjetoDbContext db,
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
                // 0. Validar a entrada: a seed nao passa pelas validacoes do AuthController
                if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Email))
                {
                    return new SeedResultDto
                    {
                        Success = false,
                        Message = "Seed ignorada: AdminUsername e AdminEmail sao obrigatorios."
                    };
                }

                if (string.IsNullOrEmpty(dto.Password) || dto.Password.Length < TamanhoMinimoSenha)
                {
                    return new SeedResultDto
                    {
                        Success = false,
                        Message = $"Seed ignorada: AdminPassword deve ter no minimo {TamanhoMinimoSenha} caracteres."
                    };
                }

                // 0.1 So executa com a tabela Users vazia (primeira execucao do sistema)
                if (await _db.Users.AnyAsync())
                {
                    return new SeedResultDto
                    {
                        Success = false,
                        Message = "Seed ignorada: a tabela Users ja possui usuarios."
                    };
                }

                // 1. Verificar se ja existe (protecao extra contra e-mail duplicado em acesso simultaneo)
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
