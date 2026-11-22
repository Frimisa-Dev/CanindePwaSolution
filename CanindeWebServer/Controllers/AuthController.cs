using CanindePwaShared.Enums;
using CanindePwaShared.Models.Auth;
using CanindePwaShared.Models.Dto.AuthDto;
using CanindePwaShared.Models.Dto.UserDto;
using CanindeWebServer.Data;
using CanindeWebServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CanindeWebServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Default: Cookies HTTPOnly (seguranca maxima)
    public class AuthController : ControllerBase
    {
        private readonly IAuthManager _auth;
        private readonly ProjetoDbContext _db;
        private readonly IEmailService _email;
        private readonly IConfiguration _cfg;
        private readonly ITempPasswordService _tempPasswordService;
        private readonly ILogger<AuthController> _logger;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IPinConfigurationService _pinConfigService;
        private readonly IServiceScopeFactory _scopeFactory;

        private static readonly SemaphoreSlim _registerLock = new SemaphoreSlim(1, 1);

        // Serializa a redefinicao de senha: o token vale UMA vez, mesmo com dois pedidos simultaneos.
        private static readonly SemaphoreSlim _resetLock = new SemaphoreSlim(1, 1);

        // Mesma resposta do forgotpassword para qualquer e-mail (nao revela quem e usuario).
        private const string MensagemRecuperacaoPadrao =
            "Se o e-mail estiver cadastrado, você receberá as instruções em instantes.";

        // Tentativas erradas de PIN por usuário (só em memória; zera ao trocar o PIN, ao ativar ou ao reiniciar).
        private const int MaxTentativasPin = 5;
        private static readonly ConcurrentDictionary<int, (string Code, int Count)> _tentativasPin = new();

        public AuthController(
            IAuthManager auth,
            ProjetoDbContext db,
            IEmailService email,
            IConfiguration cfg,
            ITempPasswordService tempPasswordService,
            ILogger<AuthController> logger,
            IPasswordHasher passwordHasher,
            IPinConfigurationService pinConfigService,
            IServiceScopeFactory scopeFactory)
        {
            _auth = auth;
            _db = db;
            _email = email;
            _cfg = cfg;
            _tempPasswordService = tempPasswordService;
            _logger = logger;
            _passwordHasher = passwordHasher;
            _pinConfigService = pinConfigService;
            _scopeFactory = scopeFactory;
        }

        // ============================================
        // ENDPOINTS PUBLICOS (AllowAnonymous)
        // ============================================

        /// <summary>
        /// Login principal - usa cookies HTTPOnly (SEGURANCA BANCARIA)
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting("Login")] // 10 tentativas por IP / 5 min (policy em Program.cs)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            var user = await _auth.ValidateUserByEmailAsync(request.Email, request.Password);
            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Credenciais invalidas."
                });
            }

            // Verificar se precisa de PIN
            if (!user.IsActive)
            {
                // ============================================
                // ✅ USUÁRIO INATIVO - CRIAR COOKIE COM AuthStage
                // ============================================

                var claimsInactive = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.TipoUsuario),
                    new Claim("AuthMode", "Cookie"),
                    new Claim("IsActive", "False"),
                    new Claim("AuthStage", "PendingPin")
                };

                var identityInactive = new ClaimsIdentity(claimsInactive, CookieAuthenticationDefaults.AuthenticationScheme);
                var principalInactive = new ClaimsPrincipal(identityInactive);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principalInactive,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(15)
                    }
                );

                return Ok(new
                {
                    success = true,
                    message = "Usuário precisa validar PIN.",
                    requiresPin = true,
                    user = new
                    {
                        user.Id,
                        user.Username,
                        user.Email,
                        user.TipoUsuario
                    }
                });
            }

            // ============================================
            // ✅ USUÁRIO ATIVO - COOKIE NORMAL
            // ============================================
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.TipoUsuario),
                new Claim("AuthMode", "Cookie"),
                new Claim("IsActive", "True")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(15)
                }
            );

            return Ok(new
            {
                success = true,
                message = "Login realizado com sucesso.",
                requiresPin = false,
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.TipoUsuario
                }
            });
        }

        /// <summary>
        /// Refresh token via cookies HTTPOnly
        /// </summary>
        [AllowAnonymous]
        [HttpPost("refreshtoken")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["caninde_refresh_token"];
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Unauthorized(new { message = "Refresh token ausente." });

            var refreshHash = ToSha256(refreshToken);
            var stored = await _auth.GetRefreshTokenByHashAsync(refreshHash);

            if (stored == null || stored.ExpiresAt < DateTime.UtcNow)
                return Unauthorized(new { message = "Refresh token invalido ou expirado." });

            var user = await _db.Users.FindAsync(stored.UserId);
            if (user == null || !user.IsActive || user.IsLocked)
                return Unauthorized(new { message = "Usuario invalido." });

            // Refresh Token Rotation
            var newRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var newHash = ToSha256(newRefreshToken);
            await _auth.RevokeRefreshTokenAsync(stored, newHash);

            var newExpires = DateTime.UtcNow.AddDays(7);
            await _auth.SaveRefreshTokenAsync(user.Id, newRefreshToken, newExpires, GetClientIp());

            // ⚠️ REMOVER geração de Access Token
            // var (accessToken, _) = await _auth.GenerateTokensAsync(user); // ❌ REMOVER

            // Atualizar cookies - APENAS Refresh Token
            //Response.Cookies.Append("caninde_refresh_token", newRefreshToken, new CookieOptions
            //{
            //    HttpOnly = true,
            //    Secure = true,
            //    SameSite = SameSiteMode.Strict, // ✅ CORRIGIDO
            //    Expires = newExpires,
            //    Path = "/",
            //    IsEssential = true
            //});

            var refreshClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("TokenType", "Refresh"),
                new Claim("TokenHash", newHash),
                new Claim("TokenId", Guid.NewGuid().ToString()) // Para revogação
            };

            await HttpContext.SignInAsync(
                "Refresh",
                new ClaimsPrincipal(new ClaimsIdentity(refreshClaims, "Refresh")),
                new AuthenticationProperties
                {
                    ExpiresUtc = newExpires,
                    IsPersistent = true,
                    AllowRefresh = false
                }
            );

            // ============================================
            // RENOVAR SESSAO COOKIE
            // ============================================
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.TipoUsuario),
                new Claim("AuthMode", "Cookie"),
                new Claim("IsActive", user.IsActive.ToString())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(15)
                }
            );

            return Ok(new
            {
                success = true,
                message = "Token renovado com sucesso."
            });
        }

        /// <summary>
        /// Registro de novo usuario
        /// </summary>
        [Authorize(Policy = "Ceo")]
        [HttpPost("cadastrarusuario")]
        public async Task<IActionResult> CadastrarUsuario([FromBody] CadastroUsuarioDto dto)
        {
            if (!await _registerLock.WaitAsync(TimeSpan.FromMilliseconds(100)))
                return Conflict(new { message = "Operacao de registro em andamento. Aguarde." });

            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (dto.Password != dto.ConfirmPassword)
                    return BadRequest(new { message = "As senhas nao conferem." });

                if (await _db.Users.AnyAsync(x => x.Email == dto.Email))
                    return BadRequest(new { message = "E-mail ja cadastrado." });

                // ?? USANDO BCrypt PARA HASH DA SENHA
                var user = new User
                {
                    Email = dto.Email,
                    PasswordHash = _passwordHasher.HashPassword(dto.Password), // ?? BCrypt
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow,
                    Username = dto.Username,
                    TipoUsuario = dto.TipoUsuario.ToString()
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                await _tempPasswordService.StoreTempPasswordAsync(user.Id, dto.Password);

                var roleName = await _auth.MapTipoUsuarioToRole(dto.TipoUsuario);
                await _auth.AssignRoleToUserAsync(user.Id, roleName);

                var pin = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(); // PIN seguro
                var expiresAt = await _pinConfigService.CalculatePinExpiration();

                _db.EmailPins.Add(new EmailPin
                {
                    UserId = user.Id,
                    Code = pin,
                    ExpiresAt = expiresAt
                });

                await _db.SaveChangesAsync();

                var minutosPin = await _pinConfigService.GetPinExpirationMinutes();
                var html = $@"
                    <h3>Bem-vindo(a), {dto.Username}!</h3>
                    <p>Seu codigo de confirmacao e:</p>
                    <h2 style='color:#2d89ef; font-size:28px'>{pin}</h2>
                    <p>Este codigo expira em {minutosPin} minutos.</p>
                    <p><strong>Senha temporaria:</strong> {dto.Password}</p>
                    <p>No primeiro acesso voce informara o codigo acima e criara a sua senha definitiva.</p>
                    <p><strong>Tipo de usuario:</strong> {dto.TipoUsuario}</p>
                ";

                await _email.SendAsync(dto.Email, "Codigo de Confirmacao - Caninde", html);

                return Ok(new CadastroUsuarioResponseDto
                {
                    Success = true,
                    Message = "Registro realizado com sucesso, verifique o e-mail e informe o código PIN",
                    UserId = user.Id,
                    RequiresPin = true
                });


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CADASTRARUSUARIO] Erro ao cadastrar usuario");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Erro interno no servidor."
                });
            }
            finally
            {
                _registerLock.Release();
            }
        }

        /// <summary>
        /// DESATIVADO: ativava a conta so com e-mail + codigo, sem criar a senha nova.
        /// A unica forma de ativar a conta e POST /api/auth/validatepin (logado como PendingPin).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("verifypin")]
        public IActionResult VerifyPin([FromBody] VerifyPinDto dto)
        {
            return StatusCode(StatusCodes.Status410Gone, new
            {
                success = false,
                message = "Ativacao por este caminho foi desativada. Faca login e informe o PIN e a nova senha."
            });
        }

        /// <summary>
        /// Verificar status do PIN (publico)
        /// </summary>
        [AllowAnonymous]
        [HttpGet("pinstatus")]
        public async Task<IActionResult> GetPinStatus(string email)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound(new { message = "Usuário não encontrado." });

            var pendingPin = await _db.EmailPins
                .Where(p => p.UserId == user.Id && !p.Used)
                .OrderByDescending(p => p.ExpiresAt)
                .FirstOrDefaultAsync();

            if (pendingPin == null)
            {
                return Ok(new PinStatusResponseDto
                {
                    HasPendingPin = false,
                    IsExpired = false,
                    UserEmail = user.Email
                });
            }

            var isExpired = pendingPin.ExpiresAt < DateTime.UtcNow;
            var minutesRemaining = isExpired ? 0 : (int)(pendingPin.ExpiresAt - DateTime.UtcNow).TotalMinutes;

            return Ok(new PinStatusResponseDto
            {
                HasPendingPin = true,
                IsExpired = isExpired,
                MinutesRemaining = minutesRemaining,
                UserEmail = user.Email,
                ExpiresAt = pendingPin.ExpiresAt
            });
        }

        /// <summary>
        /// Verificar status do PIN do usuário autenticado
        /// </summary>
        [Authorize(Policy = "AllowPendingPin")]
        [HttpGet("mypinstatus")]
        public async Task<ActionResult<PinStatusResponseDto>> GetMyPinStatus()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new PinStatusResponseDto());
            }

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new PinStatusResponseDto());
            }

            var pendingPin = await _db.EmailPins
                .Where(p => p.UserId == userId && !p.Used)
                .OrderByDescending(p => p.ExpiresAt)
                .FirstOrDefaultAsync();

            if (pendingPin == null)
            {
                return Ok(new PinStatusResponseDto
                {
                    HasPendingPin = false,
                    IsExpired = false,
                    UserEmail = user.Email
                });
            }

            var isExpired = pendingPin.ExpiresAt < DateTime.UtcNow;
            var minutesRemaining = isExpired ? 0 : (int)(pendingPin.ExpiresAt - DateTime.UtcNow).TotalMinutes;

            return Ok(new PinStatusResponseDto
            {
                HasPendingPin = true,
                IsExpired = isExpired,
                MinutesRemaining = minutesRemaining,
                UserEmail = user.Email,
                ExpiresAt = pendingPin.ExpiresAt
            });
        }

        /// <summary>
        /// Reenviar PIN para usu¨¢rio autenticado (pendente de PIN)
        /// Endpoint: POST /api/auth/resendpin
        /// Policy: AllowPendingPin (usu¨¢rio autenticado com cookie, IsActive pode ser false)
        /// Body: { "email": "string" } (opcional - apenas para valida??o)
        /// </summary>
        [Authorize(Policy = "AllowPendingPin")]
        [HttpPost("resendpin")]
        public async Task<IActionResult> ResendPin([FromBody] ResendPinDto? dto = null)
        {
            try
            {
                // 1. OBTER USU¨¢RIO AUTENTICADO (do cookie HTTPOnly)
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Não autenticado. Faça login novamente."
                    });
                }

                // 2. BUSCAR USU¨¢RIO NO BANCO
                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Usuário não encontrado no sistema."
                    });
                }

                // 3. VALIDAR EMAIL (se fornecido no DTO)
                if (dto != null && !string.IsNullOrWhiteSpace(dto.Email))
                {
                    if (!string.Equals(user.Email, dto.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "E-mail não corresponde ao usuário autenticado."
                        });
                    }
                }

                // 4. VERIFICAR SE USU¨¢RIO PRECISA DE PIN
                // (AllowPendingPin permite IsActive = false, mas verificamos l¨®gica)
                var hasPendingPin = await _db.EmailPins
                    .AnyAsync(p => p.UserId == userId && !p.Used && p.ExpiresAt > DateTime.UtcNow);

                if (!hasPendingPin && user.IsActive)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Usuário já está ativo e não tem PIN pendente."
                    });
                }

                // 5. INVALIDAR PINs ANTIGOS DO USU¨¢RIO
                var oldPins = await _db.EmailPins
                    .Where(p => p.UserId == userId && !p.Used)
                    .ToListAsync();

                foreach (var pin in oldPins)
                {
                    pin.Used = true;
                }

                // 6. GERAR NOVO PIN (6 d¨ªgitos)
                var newPin = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                var expiresAt = await _pinConfigService.CalculatePinExpiration();

                _db.EmailPins.Add(new EmailPin
                {
                    UserId = userId,
                    Code = newPin,
                    ExpiresAt = expiresAt,
                    Used = false
                });

                await _db.SaveChangesAsync();

                // 7. OBTER SENHA TEMPOR¨¢RIA (se existir - para novos usu¨¢rios)
                var tempPassword = await _tempPasswordService.GetAndInvalidateTempPasswordAsync(userId);

                // 8. ENVIAR EMAIL
                await _email.SendAsync(
                    user.Email,
                    "Seu novo código de acesso - Caninde",
                    $"""
                        <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                            <h2 style="color: #2b5cd8;">Ol¨¢ {user.Username}!</h2>
                            <p>Seu novo código de confirmação ¨¦:</p>                
                            <div style="background: #f5f5f5; padding: 20px; border-radius: 8px; text-align: center; margin: 20px 0;">
                                <h1 style="color: #2b5cd8; font-size: 36px; letter-spacing: 4px; margin: 0;">
                                    {newPin}
                                </h1>
                            </div>
                
                            <p><strong>E-mail:</strong> {user.Email}</p>
                            <p><strong>Tipo de Usuário:</strong> {user.TipoUsuario}</p>
                            {(tempPassword != null ?
                            $"<p><strong>Senha Temporária:</strong> {tempPassword}</p>" :
                            "<p>Use a senha que você cadastrou.</p>")}
                
                            <p>? Este código expira em {await _pinConfigService.GetPinExpirationMinutes()} minutos.</p>
                
                            <hr style="border: none; border-top: 1px solid #eee; margin: 20px 0;">
                
                            <p style="color: #666; font-size: 12px;">
                                Se você não solicitou este código, ignore este e-mail.
                            </p>
                        </div>
                    """
                );

                return Ok(new
                {
                    success = true,
                    message = $"Novo PIN enviado para {user.Email}.",
                    expiresAt = expiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RESENDPIN] Erro ao processar solicitação");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro interno ao processar solicitação."
                });
            }
        }

        /// <summary>
        /// Listar PINs pendentes (apenas admin)
        /// </summary>
        [Authorize(Policy = "CeoAdmin")]
        [HttpGet("pendingpins")]
        public async Task<IActionResult> GetPendingPins()
        {
            var pins = await _db.EmailPins
                .Include(p => p.User)
                .Where(p => !p.Used)
                .Select(p => new PendingPinDto
                {
                    Username = p.User.Username,
                    Email = p.User.Email,
                    ExpiresAt = p.ExpiresAt,
                    IsExpired = p.ExpiresAt < DateTime.UtcNow,
                    MinutesRemaining = p.ExpiresAt < DateTime.UtcNow
                        ? 0
                        : (int)(p.ExpiresAt - DateTime.UtcNow).TotalMinutes
                })
                .ToListAsync();

            return Ok(pins);
        }

        /// <summary>DESATIVADO: o link de confirmacao ativava a conta sem criar a senha nova.</summary>
        [AllowAnonymous]
        [HttpPost("resendconfirmation")]
        public IActionResult ResendConfirmation([FromBody] ResendPinDto dto)
        {
            return StatusCode(StatusCodes.Status410Gone, new
            {
                success = false,
                message = "Confirmacao por link foi desativada. Faca login e informe o PIN e a nova senha."
            });
        }

        /// <summary>DESATIVADO: ver resendconfirmation.</summary>
        [AllowAnonymous]
        [HttpGet("confirm")]
        public IActionResult Confirm(string token, string email)
        {
            return StatusCode(StatusCodes.Status410Gone, new
            {
                success = false,
                message = "Confirmacao por link foi desativada. Faca login e informe o PIN e a nova senha."
            });
        }

        /// <summary>
        /// Recuperacao de senha - PASSO 1: pede o link de redefinicao por e-mail.
        /// - RESPOSTA SEMPRE IGUAL (exista o e-mail ou nao): nao revela quem e usuario.
        /// - So usuario ATIVO e nao bloqueado recebe o e-mail; quem esta pendente de PIN segue pelo fluxo do PIN.
        /// - Intervalo minimo entre pedidos (PinResendCooldownMinutes); novo pedido invalida os links anteriores.
        /// - Validade = mesma do PIN (PinExpirationMinutes, padrao 30 min).
        /// - O link usa SiteBaseUrl (configuracao), NUNCA o Host da requisicao (evita link envenenado),
        ///   com o token no FRAGMENTO (#token=...), que nao vai para logs do servidor nem para o Referer.
        /// - O e-mail sai em segundo plano: o tempo de resposta nao denuncia se o e-mail existe.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting("RecuperacaoSenha")] // 5 pedidos por IP / 10 min (policy em Program.cs)
        [HttpPost("forgotpassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            try
            {
                var email = dto.Email.Trim();
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

                // Desconhecido, pendente de PIN ou bloqueado: mesma resposta, nada e enviado.
                if (user == null || !user.IsActive || user.IsLocked)
                    return Ok(new { success = true, message = MensagemRecuperacaoPadrao });

                // Intervalo minimo entre pedidos (conta tambem links ja revogados).
                var cooldownMinutos = await _pinConfigService.GetPinResendCooldownMinutes();
                var ultimoPedido = await _auth.GetLastResetPasswordRequestAtAsync(user.Id);

                if (ultimoPedido.HasValue &&
                    DateTime.UtcNow - ultimoPedido.Value < TimeSpan.FromMinutes(cooldownMinutos))
                    return Ok(new { success = true, message = MensagemRecuperacaoPadrao });

                var siteBase = ObterSiteBaseUrl();
                if (siteBase == null)
                {
                    _logger.LogError("[FORGOTPASSWORD] SiteBaseUrl ausente ou nao HTTPS: e-mail de redefinicao NAO enviado.");
                    return Ok(new { success = true, message = MensagemRecuperacaoPadrao });
                }

                // Links anteriores deixam de valer; so o mais novo funciona.
                await _auth.RevokeUserTokensAsync(user.Id, "PasswordReset");

                // 256 bits aleatorios em Base64Url (43 caracteres, seguro para URL).
                // No banco fica so o hash SHA-256 do token.
                var token = Microsoft.AspNetCore.WebUtilities.Base64UrlTextEncoder.Encode(RandomNumberGenerator.GetBytes(32));
                var expiresAt = await _pinConfigService.CalculatePinExpiration();

                await _auth.SaveResetPasswordTokenAsync(user.Id, token, expiresAt, GetClientIp());

                var minutos = await _pinConfigService.GetPinExpirationMinutes();
                var link = $"{siteBase}/redefinir-senha#token={Uri.EscapeDataString(token)}";

                var pedidoEm = FormatarDataHoraLocal(DateTime.UtcNow);
                var pedidoIp = System.Net.WebUtility.HtmlEncode(GetClientIp());

                var html = $"""
                    <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                        <h2 style="color: #2b5cd8;">Olá {System.Net.WebUtility.HtmlEncode(user.Username)}!</h2>
                        <p>Recebemos um pedido para redefinir a senha da sua conta.</p>
                        <p>
                            <a href="{link}" style="display: inline-block; background: #2b5cd8; color: #ffffff; padding: 12px 24px; border-radius: 6px; text-decoration: none;">
                                Redefinir senha
                            </a>
                        </p>
                        <p>Se o botão não funcionar, copie e cole este endereço no navegador:<br>{link}</p>
                        <p>Este link expira em {minutos} minutos e só pode ser usado uma vez.</p>
                        <p style="color: #666; font-size: 12px;">
                            Pedido feito em {pedidoEm} a partir do IP {pedidoIp}.<br>
                            Se você não pediu a redefinição, ignore este e-mail: sua senha continua a mesma.
                        </p>
                    </div>
                    """;

                EnviarEmailEmSegundoPlano(user.Email, "Redefinição de senha - Caninde", html);
            }
            catch (Exception ex)
            {
                // Erro interno NAO muda a resposta (um 500 so para e-mails existentes revelaria o cadastro).
                _logger.LogError(ex, "[FORGOTPASSWORD] Erro ao processar solicitacao");
            }

            return Ok(new { success = true, message = MensagemRecuperacaoPadrao });
        }

        /// <summary>
        /// Recuperacao de senha - PASSO 2: recebe o token do link + a senha nova.
        /// - O token vale UMA vez e expira; o servidor compara o HASH (nunca guarda o token).
        /// - Senha fora das regras: 400 com a lista de erros e o token CONTINUA valido para nova tentativa.
        /// - Sucesso: grava a senha nova e consome o token numa unica gravacao, depois cancela os
        ///   outros links pendentes e as sessoes de refresh do usuario.
        /// - Nao entra automaticamente: a pessoa vai para o login.
        /// - Limite: o cookie de sessao ja emitido (15 min) nao e revogavel no servidor; expira sozinho.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting("RedefinicaoSenha")] // 10 tentativas por IP / 10 min (policy em Program.cs)
        [HttpPost("resetpassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            await _resetLock.WaitAsync();
            try
            {
                var tokenHash = ToSha256(dto.Token.Trim());
                var stored = await _auth.GetResetPasswordTokenAsync(tokenHash);

                // Mesma mensagem para token inexistente, expirado, usado ou de conta que nao pode redefinir.
                var user = stored == null ? null : await _db.Users.FindAsync(stored.UserId);

                if (stored == null || user == null || !user.IsActive || user.IsLocked)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Link inválido ou expirado. Solicite um novo link.",
                        invalidToken = true
                    });
                }

                var erros = ValidarNovaSenha(dto.NewPassword, dto.ConfirmPassword, user.PasswordHash);
                if (erros.Count > 0)
                {
                    // Token NAO e consumido: a pessoa corrige a senha e tenta de novo.
                    return BadRequest(new { success = false, message = "Senha nova inválida.", errors = erros });
                }

                // Uma unica gravacao: senha nova + token consumido.
                user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
                stored.RevokedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                // Depois da troca: outros links pendentes e sessoes de refresh deixam de valer.
                await _auth.RevokeUserTokensAsync(user.Id, "PasswordReset");
                await _auth.RevokeUserTokensAsync(user.Id, "Authentication");

                _logger.LogInformation("[RESETPASSWORD] Senha redefinida para o usuario {UserId}", user.Id);

                // Se o token foi roubado, o dono da conta fica sabendo na hora.
                EnviarAvisoSenhaAlteradaEmSegundoPlano(user);

                return Ok(new
                {
                    success = true,
                    message = "Senha redefinida com sucesso. Faça login com a nova senha."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RESETPASSWORD] Erro ao redefinir senha");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro interno ao redefinir a senha."
                });
            }
            finally
            {
                _resetLock.Release();
            }
        }

        /// <summary>
        /// Homologacao do novo usuario (APENAS para AuthStage = PendingPin).
        /// Recebe o PIN do e-mail + a senha definitiva. So ativa a conta se OS DOIS estiverem corretos;
        /// qualquer falha nao grava nada. PIN errado: 5 tentativas, depois um novo PIN e enviado.
        /// </summary>
        [Authorize(Policy = "AllowPendingPin")]
        [HttpPost("validatepin")]
        public async Task<IActionResult> ValidatePin([FromBody] ValidatePinDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { success = false, message = "Não autenticado." });
            }

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "Usuário não encontrado." });
            }

            if (user.IsActive)
            {
                return BadRequest(new { success = false, message = "Conta já está ativa." });
            }

            var pinEntry = await _db.EmailPins
                .Where(p => p.UserId == userId && !p.Used)
                .OrderByDescending(p => p.ExpiresAt)
                .FirstOrDefaultAsync();

            if (pinEntry == null)
            {
                return BadRequest(new { success = false, message = "Nenhum PIN encontrado. Solicite um novo código." });
            }

            if (pinEntry.ExpiresAt < DateTime.UtcNow)
            {
                return BadRequest(new { success = false, message = "Código expirado. Solicite um novo código.", expired = true });
            }

            // 1) PIN
            if (!PinConfere(pinEntry.Code, dto.Code))
            {
                var tentativas = _tentativasPin.AddOrUpdate(
                    user.Id,
                    _ => (pinEntry.Code, 1),
                    (_, atual) => atual.Code == pinEntry.Code ? (atual.Code, atual.Count + 1) : (pinEntry.Code, 1)).Count;

                if (tentativas >= MaxTentativasPin)
                {
                    _tentativasPin.TryRemove(user.Id, out _);
                    await GerarNovoPinEEnviarAsync(user);
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Limite de {MaxTentativasPin} tentativas atingido. Um novo código foi enviado para o seu e-mail.",
                        attemptsRemaining = 0,
                        newPinSent = true
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = "Código inválido.",
                    attemptsRemaining = MaxTentativasPin - tentativas
                });
            }

            // 2) Senha nova (PIN correto, mas a senha precisa passar nas regras)
            var erros = ValidarNovaSenha(dto.NewPassword, dto.ConfirmPassword, user.PasswordHash);
            if (erros.Count > 0)
            {
                return BadRequest(new { success = false, message = "Senha nova inválida.", errors = erros });
            }

            // 3) Uma única gravação: senha + ativação + PIN usado
            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
            user.IsActive = true;
            pinEntry.Used = true;

            _db.Users.Update(user);
            _db.EmailPins.Update(pinEntry);
            await _db.SaveChangesAsync();

            _tentativasPin.TryRemove(user.Id, out _);

            // A senha temporária (enviada por e-mail) não vale mais.
            try { await _tempPasswordService.GetAndInvalidateTempPasswordAsync(userId); }
            catch (Exception ex) { _logger.LogWarning(ex, "[VALIDATEPIN] Falha ao invalidar senha temporária do usuário {UserId}", userId); }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.TipoUsuario),
                new Claim("AuthMode", "Cookie"),
                new Claim("IsActive", "True")
                // NÃO incluir AuthStage - usuário agora é ativo
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(15)
                }
            );

            return Ok(new
            {
                success = true,
                message = "Conta ativada com sucesso!",
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.TipoUsuario,
                    IsActive = true
                }
            });
        }

        // ============================================
        // ENDPOINTS AUTENTICADOS (Require Auth)
        // ============================================

        /// <summary>
        /// Logout bancário real:
        /// - Remove cookie de autenticação
        /// - Remove cookie de refresh (se existir)
        /// - Invalida sessão no middleware
        /// </summary>
        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // 1. Remove cookie principal de autenticação
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            // 2. Defesa adicional: delete explícito (opcional, mas seguro)
            Response.Cookies.Delete("caninde_access_token", new CookieOptions
            {
                Path = "/",
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.None
            });

            return Ok(new
            {
                Success = true,
                Message = "Sessão encerrada com segurança."
            });
        }

        /// <summary>
        /// Obter perfil do usuário (ÚNICO endpoint para todos os estados)
        /// Regras:
        /// 1. Se IsActive = true → retorna perfil completo (com permissões)
        /// 2. Se IsActive = false → retorna perfil básico (sem permissões, apenas para validação PIN)
        /// 3. Sempre requer autenticação (cookie válido)
        /// 4. Não filtra por IsActive na query (deixa a lógica decidir)
        /// </summary>
        [Authorize(Policy = "AllowInactiveUser")]
        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileResponseDto>> GetUserProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new UserProfileResponseDto
                    {
                        Success = false,
                        Message = "Não autenticado."
                    });
                }

                // ⚠️ QUERY MODIFICADA: REMOVER u.IsActive do filtro
                var user = await _db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Where(u => u.Id == userId && !u.IsLocked) // ← SEM IsActive
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.TipoUsuario,
                        u.IsActive, // ← Vamos usar esta informação
                        u.CreatedAt,
                        u.LastLoginAt,
                        Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                    })
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new UserProfileResponseDto
                    {
                        Success = false,
                        Message = "Usuário não encontrado."
                    });
                }

                var userProfile = new UserProfileDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    TipoUsuario = user.TipoUsuario,
                    Roles = user.Roles.ToArray(),
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    IsActive = user.IsActive // ← ESTADO REAL
                };

                // ⚠️ LÓGICA DECISÓRIA: Se IsActive = true, adiciona permissões
                if (user.IsActive)
                {
                    userProfile.Permissions = GetUserPermissions(user.Roles);

                    return Ok(new UserProfileResponseDto
                    {
                        Success = true,
                        Message = "Perfil obtido com sucesso.",
                        User = userProfile
                    });
                }
                else
                {
                    // IsActive = false → apenas perfil básico para validação PIN
                    return Ok(new UserProfileResponseDto
                    {
                        Success = true,
                        Message = "Perfil obtido (usuário pendente de PIN).",
                        User = userProfile
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PROFILE] Erro");
                return StatusCode(500, new UserProfileResponseDto
                {
                    Success = false,
                    Message = "Erro interno."
                });
            }
        }

        /// <summary>
        /// Alterar senha (com BCrypt)
        /// </summary>
        [HttpPost("changepassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized();

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            // ?? Verificar senha atual com BCrypt
            if (!_passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                return BadRequest(new { message = "Senha atual incorreta." });

            var errosSenha = ValidarNovaSenha(dto.NewPassword, dto.NewPassword, user.PasswordHash);
            if (errosSenha.Count > 0)
                return BadRequest(new { success = false, message = "Senha nova inválida.", errors = errosSenha });

            // ?? Nova senha com BCrypt
            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Senha alterada com sucesso." });
        }

        // ============================================
        // METODOS AUXILIARES PRIVADOS
        // ============================================
        private List<string> GetUserPermissions(List<string> roles)
        {
            var permissions = new List<string>();

            foreach (var role in roles)
            {
                switch (role.ToLower())
                {
                    case "ceo":
                        permissions.AddRange(new[] { "full_access", "user_management", "financial_reports", "system_config" });
                        break;
                    case "administrador":
                        permissions.AddRange(new[] { "user_management", "product_management", "order_management" });
                        break;
                    case "financeiro":
                        permissions.AddRange(new[] { "product_view", "order_create", "customer_management" });
                        break;
                }
            }

            return permissions.Distinct().ToList();
        }
       
        /// <summary>
        /// Regras da senha: minimo 8 e maximo 64 caracteres, maiuscula, minuscula, numero e simbolo;
        /// igual a confirmacao; diferente da senha atual. Retorna a lista de problemas (vazia = ok).
        /// </summary>
        private List<string> ValidarNovaSenha(string? nova, string? confirmacao, string hashAtual)
        {
            var erros = new List<string>();

            if (string.IsNullOrEmpty(nova))
            {
                erros.Add("Informe a nova senha.");
                return erros;
            }

            if (nova.Length < 8) erros.Add("A senha deve ter no mínimo 8 caracteres.");
            if (nova.Length > 64) erros.Add("A senha deve ter no máximo 64 caracteres.");
            if (!nova.Any(char.IsUpper)) erros.Add("A senha deve ter ao menos uma letra maiúscula.");
            if (!nova.Any(char.IsLower)) erros.Add("A senha deve ter ao menos uma letra minúscula.");
            if (!nova.Any(char.IsDigit)) erros.Add("A senha deve ter ao menos um número.");
            if (!nova.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c))) erros.Add("A senha deve ter ao menos um símbolo (ex.: @ # $ %).");
            if (nova != confirmacao) erros.Add("A confirmação da senha não confere.");
            if (_passwordHasher.VerifyPassword(nova, hashAtual)) erros.Add("A nova senha deve ser diferente da senha atual.");

            return erros;
        }

        /// <summary>Comparação do PIN em tempo constante.</summary>
        private static bool PinConfere(string? esperado, string? informado)
        {
            if (esperado == null || informado == null) return false;
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(esperado), Encoding.UTF8.GetBytes(informado));
        }

        /// <summary>Invalida os PINs pendentes, gera um novo e envia por e-mail (limite de tentativas atingido).</summary>
        private async Task GerarNovoPinEEnviarAsync(User user)
        {
            var antigos = await _db.EmailPins.Where(p => p.UserId == user.Id && !p.Used).ToListAsync();
            foreach (var p in antigos) p.Used = true;

            var novoPin = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var expiresAt = await _pinConfigService.CalculatePinExpiration();

            _db.EmailPins.Add(new EmailPin
            {
                UserId = user.Id,
                Code = novoPin,
                ExpiresAt = expiresAt,
                Used = false
            });
            await _db.SaveChangesAsync();

            var minutos = await _pinConfigService.GetPinExpirationMinutes();
            await _email.SendAsync(
                user.Email,
                "Novo código de acesso - Caninde",
                $"""
                <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                    <h2 style="color: #2b5cd8;">Olá {user.Username}!</h2>
                    <p>O limite de tentativas do código anterior foi atingido. Seu novo código de confirmação é:</p>
                    <h1 style="color: #2b5cd8; font-size: 36px; letter-spacing: 4px;">{novoPin}</h1>
                    <p>Este código expira em {minutos} minutos.</p>
                    <p style="color: #666; font-size: 12px;">Se você não solicitou este código, ignore este e-mail.</p>
                </div>
                """);
        }

        /// <summary>
        /// URL do site (chave SiteBaseUrl do Registry, gravada pelo Start-Dev), sem barra final.
        /// Precisa ser absoluta e HTTPS; senao devolve null e o link NAO e enviado.
        /// </summary>
        private string? ObterSiteBaseUrl()
        {
            var url = _cfg["SiteBaseUrl"]?.Trim();

            if (string.IsNullOrWhiteSpace(url) ||
                !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps)
                return null;

            return url.TrimEnd('/');
        }

        /// <summary>
        /// Avisa o dono da conta que a senha foi alterada (quando e de qual IP), para ele reagir
        /// se nao foi ele. Reutilizado pela redefinicao e, depois, pela troca de senha.
        /// Os dados do usuario e da requisicao sao lidos AGORA (antes do segundo plano).
        /// </summary>
        private void EnviarAvisoSenhaAlteradaEmSegundoPlano(User user)
        {
            var para = user.Email;
            var nome = System.Net.WebUtility.HtmlEncode(user.Username);
            var quando = FormatarDataHoraLocal(DateTime.UtcNow);
            var ip = System.Net.WebUtility.HtmlEncode(GetClientIp());

            var html = $"""
                <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                    <h2 style="color: #2b5cd8;">Olá {nome}!</h2>
                    <p>A senha da sua conta foi alterada em <strong>{quando}</strong>, a partir do IP {ip}.</p>
                    <p>Se foi você, nenhuma ação é necessária.</p>
                    <p><strong>Se não foi você</strong>, avise o administrador do sistema imediatamente.</p>
                </div>
                """;

            EnviarEmailEmSegundoPlano(para, "Sua senha foi alterada - Caninde", html);
        }

        /// <summary>
        /// Data/hora para exibir em e-mails, no horario de Brasilia (Windows ou Linux).
        /// Se o fuso nao existir na maquina, mostra em UTC e diz isso no texto.
        /// </summary>
        private static string FormatarDataHoraLocal(DateTime utc)
        {
            var momento = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

            foreach (var id in new[] { "E. South America Standard Time", "America/Sao_Paulo" })
            {
                try
                {
                    var fuso = TimeZoneInfo.FindSystemTimeZoneById(id);
                    return TimeZoneInfo.ConvertTimeFromUtc(momento, fuso).ToString("dd/MM/yyyy HH:mm") + " (horário de Brasília)";
                }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }

            return momento.ToString("dd/MM/yyyy HH:mm") + " (UTC)";
        }

        /// <summary>
        /// Envia o e-mail depois de responder a requisicao (escopo proprio: o DbContext da requisicao
        /// ja foi descartado). Falha so vai para o log.
        /// </summary>
        private void EnviarEmailEmSegundoPlano(string para, string assunto, string html)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    await email.SendAsync(para, assunto, html);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EMAIL] Falha ao enviar e-mail em segundo plano: {Assunto}", assunto);
                }
            });
        }

        private string GetClientIp()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        }
        
        private static string ToSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }


        // ============================================
        // ENDPOINTS DE TESTE/DEBUG
        // ============================================

        /// <summary>
        /// Health check
        /// </summary>
        [AllowAnonymous]
        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            var dbConnected = await _db.Database.CanConnectAsync();

            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                https = HttpContext.Request.IsHttps,
                database = dbConnected ? "connected" : "disconnected"
            });
        }

    }
}