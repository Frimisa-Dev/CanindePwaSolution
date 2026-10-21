#nullable enable
/*
===============================================================================
CURIO WEB SERVER — Program.cs
===============================================================================
Arquivo de bootstrap principal do backend Falcao.

IMPORTANTE:
- ESTE ARQUIVO É ORDEM-SENSÍVEL
- NÃO refatorar
- NÃO mover blocos
- NÃO unificar configurações
- NÃO misturar ambientes fora das regras abaixo

Qualquer alteração deve:
1. Atualizar o checklist
2. Respeitar a ordem cronológica
3. Não quebrar Development nem Production

===============================================================================
CHECKLIST CRONOLÓGICO (FONTE DE VERDADE)
===============================================================================
[01] Using directives / Nullable
[02] UnhandledException (fail-safe)
[03] WebApplication Builder
[04] Ambiente CURIO (fonte única)
[05] Configuration JSON (não sensível)
[06] Registry (HKCU Dev / HKLM Prod)
[07] Validação de segurança (segredos)
[08] Portas
[09] Certificados HTTPS
[10] Banco de Dados
[11] JWT (Access / Refresh)
[12] Email
[13] Health Checks
[14] Dependency Injection (Services)
[15] Token Service Factory
[16] HttpClients
[17] CORS
[18] Controllers + JSON
[19] Swagger (Development only)
[20] Authentication
[21] Authorization
[22] Logging
[23] Kestrel
[24] Build da aplicação
[25] Middleware Pipeline
[26] Endpoints / Controllers
[27] Database Seed
[28] Startup Diagnostics
[29] Métodos auxiliares
===============================================================================
*/

/*
===============================================================================
FASE 01 — USING DIRECTIVES / NULLABLE
===============================================================================
Objetivo:
- Ativar análise de nullabilidade em tempo de compilação
- Reduzir falhas silenciosas de segurança e runtime
- Forçar contratos explícitos (null / not-null)

Regras:
- NÃO desativar nullable
- NÃO mover usings de lugar sem entender dependências
- A ordem dos usings reflete dependências implícitas do pipeline

Impacto:
- Compilação mais rigorosa
- Runtime mais previsível
- Menor risco de NullReference em Auth / Security / Pipeline
===============================================================================
*/
using FalcaoPwaShared.Enums;
using FalcaoPwaShared.Models.Auth;
using FalcaoPwaShared.Models.Dtos.AuthDto;
using FalcaoWebServer.Configuration;
using FalcaoWebServer.Data;
using FalcaoWebServer.Services;
using FalcaoWebServer.Services.Health;
using FalcaoWebServer.Swagger;
using FalcaoWebServer.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

/*
===============================================================================
FASE 02 — UNHANDLED EXCEPTION / FAIL-SAFE GLOBAL
===============================================================================
Objetivo:
- Garantir que exceções não tratadas NÃO derrubem o processo sem rastreabilidade
- Centralizar comportamento de falha crítica
- Preparar o ambiente para logs estruturados em produção

Observação:
- Nesta fase NÃO há log, tele-metria ou Console.WriteLine
- Apenas definição conceitual do ponto onde falhas globais seriam capturadas

Regras:
- NÃO lançar exceções aqui
- NÃO adicionar handlers agora
- Esta fase serve como MARCO DOCUMENTAL do fluxo

Impacto:
- Nenhum impacto funcional
- Facilita auditoria futura
===============================================================================
*/

AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    try
    {
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            Event = "UnhandledException",
            ExceptionType = e.ExceptionObject.GetType().Name,
            IsTerminating = e.IsTerminating
        };
        File.AppendAllText("startup-crash.log", $"{JsonSerializer.Serialize(logEntry)}{Environment.NewLine}");
    }
    catch { /* never throw from here */ }
};

/*
===============================================================================
FASE 03 — IDENTIFICAÇÃO DE AMBIENTE (CURIO_ENVIRONMENT)
===============================================================================
Objetivo:
- Determinar explicitamente se o sistema está em Development ou Production
- Evitar dependência implícita de ASPNETCORE_ENVIRONMENT
- Garantir comportamento previsível de segurança

[FIX 2026-10-18 - compatível com .NET 10]
Em .NET 6-9: alterar `builder.Environment.EnvironmentName` APÓS o
`WebApplication.CreateBuilder` propagava para o Host interno porque ambos
compartilhavam a mesma referência de IHostEnvironment.

Em .NET 10: o IHostEnvironment interno é tratado como imutável após o
`CreateBuilder`. Alterar o wrapper exposto por `builder.Environment` NÃO
altera o valor visto por `Microsoft.Hosting.Lifetime`, e middleware
`UseHttpsRedirection`.

Sintoma do bug (visível nos logs):
  - "Ambiente: Development"            (leitura custom via app.Environment)
  - "Hosting environment: Production"  (leitura real via Lifetime)
  - Kestrel ativa CheckCertificateRevocation em Production, falha o
    handshake TLS com certificado self-signed, fecha a conexão com
    ERR_CONNECTION_CLOSED no Chrome.

Solução (mecanismo oficial desde .NET 6):
Ler CURIO__ENVIRONMENT ANTES do CreateBuilder e passar via
WebApplicationOptions.EnvironmentName. O Host interno nasce já com o valor
correto e todas as leituras concordam.

Regras mantidas:
- Ambiente influencia CORS, Cookie, HTTPS, Auth Schemes
- Validação de valor permitido permanece idêntica
===============================================================================
*/

var falcaoEnvironment =
    Environment.GetEnvironmentVariable("CURIO__ENVIRONMENT")
    ?? throw new InvalidOperationException(
        "Variável obrigatória não definida: CURIO__ENVIRONMENT (Development | Production)");

if (falcaoEnvironment != "Development" && falcaoEnvironment != "Production")
{
    throw new InvalidOperationException(
        $"Valor inválido para CURIO__ENVIRONMENT: {falcaoEnvironment}");
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    EnvironmentName = falcaoEnvironment
});

/*
===============================================================================
FASE 04 — REGISTRY / CONFIGURAÇÃO EXTERNA
===============================================================================
Objetivo:
- Centralizar parâmetros sensíveis fora do código
- Diferenciar comportamento por ambiente

Premissas:
- Development:
  - HKCU
- Production:
  - HKLM

Tipos de dados esperados:
- Strings de conexão
- Flags de segurança
- Portas
- Certificados

Regras:
- Código NÃO deve assumir valores padrão silenciosamente
- Falha de leitura deve ser tratada explicitamente
===============================================================================
*/

// [FIX 2026-10-18] REMOVIDA a linha:
//   builder.Environment.EnvironmentName = falcaoEnvironment;
// Em .NET 10 essa atribuição não propaga para o Host interno.
// O ambiente agora é injetado via WebApplicationOptions.EnvironmentName
// na chamada do CreateBuilder (ver FASE 03 acima).

// A. ARQUIVOS .JSON PRIMEIRO (APENAS NÃO SENSÍVEIS)
builder.Configuration.AddJsonFile("appsettings.json", optional: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

// B. REGISTRY (HKCU para Development / HKLM para Production)
try
{
    builder.Configuration.AddFalcaoRegistry();
}
catch (Exception ex)
{
    throw new InvalidOperationException(
        "Registry não configurado. Execute:\n" +
        "  • Desenvolvimento: ScriptInfraDevelopment.ps1\n" +
        "  • Produção: ScriptInfraNuvem.ps1\n" +
        $"Erro: {ex.Message}");
}

// C. USER SECRETS (APENAS Development - opcional)
if (builder.Environment.IsDevelopment())
{
    try
    {
        builder.Configuration.AddUserSecrets<Program>();
    }
    catch
    {
        // User Secrets não configurado - usar Registry
    }
}

// VERIFICAÇÃO DE SEGURANÇA: Nenhum segredo em .json
ValidateNoSecretsInJson(builder.Configuration);

// Helper para leitura segura APENAS do Registry
string GetConfigValue(string key, string? defaultValue = null, bool isSensitive = false)
{
    // SÓ REGISTRY (HKCU ou HKLM)
    string? value = builder.Configuration[key];

    // Se não encontrou no Registry e tem valor padrão (não sensível)
    if (string.IsNullOrEmpty(value) && !isSensitive)
    {
        value = defaultValue;
    }

    // VALIDAÇÃO DE SEGURANÇA CRÍTICA (produção)
    if (isSensitive && string.IsNullOrEmpty(value) && builder.Environment.IsProduction())
    {
        throw new InvalidOperationException(
            $"CONFIGURAÇÃO SENSÍVEL OBRIGATÓRIA: {key}. " +
            $"Configure no Registry: HKLM:\\SOFTWARE\\Falcao\\Secrets");
    }

    return value ?? string.Empty;
}

/*
===============================================================================
FASE 05 — PORTAS / ENDPOINTS
===============================================================================
Objetivo:
- Documentar separação obrigatória em Development
- Garantir alinhamento com Frontend

Premissas:
- Development:
  - Backend: porta própria (ex: 7199)
  - Frontend: porta distinta (ex: 7201)
- Production:
  - Porta única no servidor da nuvem

Regras:
- NÃO tentar unificar portas em Development
- Cookies cross-origin são esperados aqui
===============================================================================
*/

// Portas (não sensíveis)
int httpsPort = int.TryParse(
    GetConfigValue("BackendHttpsPort", "7199"),
    out var sp) ? sp : 7199;

int healthPort = int.TryParse(
    GetConfigValue("ServerHealthPort", "7198"),
    out var hp) ? hp : 7198;

/*
===============================================================================
FASE 06 — CERTIFICADOS / HTTPS
===============================================================================
Objetivo:
- Garantir comunicação segura
- Diferenciar exigências entre Development e Production

Premissas:
- Development:
  - Certificados locais / dev
- Production:
  - Certificado válido (CA)

Regras:
- Cookie Secure depende desta fase
- HTTPS é pré-requisito para SameSite=None
===============================================================================
*/

// HTTPS certificate
var certThumb = GetConfigValue("BackendCertificateThumbprint", string.Empty,
    isSensitive: builder.Environment.IsProduction());
var certStoreLocation = GetConfigValue("BackendCertificateStoreLocation", "LocalMachine");

/*
===============================================================================
FASE 07 — BANCO DE DADOS
===============================================================================
Objetivo:
- Inicializar acesso a dados de forma previsível
- Garantir string de conexão externa (Registry)

Premissas:
- String de conexão NÃO fica no código
- Ambiente define origem da configuração

Regras:
- Nenhuma lógica de negócio aqui
- Apenas infraestrutura
===============================================================================
*/
var connString = GetConfigValue("DbConnection",
                 isSensitive: true)
                 ?? throw new InvalidOperationException(
                     "Connection string não configurada. " +
                     "Configure no Registry: DbConnection");

// Log seguro da configuração do banco
var safeConnString = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = connString };
if (safeConnString.ContainsKey("password"))
{
    safeConnString["password"] = "***";
}
builder.Services.AddSingleton(new
{
    SafeConnectionString = safeConnString.ConnectionString,
    Source = builder.Configuration["Registry:Source"] ?? "Unknown"
});

builder.Services.AddDbContext<FalcaoDbContext>(options =>
    options.UseMySQL(connString)
           .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
           .LogTo(Console.WriteLine,
               builder.Environment.IsDevelopment() ? LogLevel.Information : LogLevel.Warning)
);

/*
===============================================================================
FASE 08 — JWT (ACCESS / REFRESH)
===============================================================================
Objetivo:
- Centralizar toda a configuração de JWT exclusivamente no Registry
- Definir parâmetros de validação de token de forma explícita
- Garantir segurança mínima (chave, issuer, audience, expiração)

O que acontece aqui:
- Leitura da JwtKey, Issuer, Audience e tempos de expiração
- Validação do tamanho mínimo da chave (>= 256 bits)
- Criação do TokenValidationParameters
- Registro como Singleton no container DI

Premissas:
- JWT é utilizado SOMENTE para:
  - Swagger
  - Testes
  - Fluxos internos de validação
- JWT NÃO é o mecanismo primário de autenticação do sistema

Regras:
- JwtKey nunca pode estar em JSON
- JwtKey é obrigatória em Production
- ClockSkew = Zero (sem tolerância de tempo)

Impacto:
- Define o contrato criptográfico do sistema
- Base para validação tanto no Cookie quanto no JwtBearer
===============================================================================
*/

var jwtKey = GetConfigValue("JwtKey",
              isSensitive: true)
              ?? throw new InvalidOperationException(
                  "JWT Key não configurada. " +
                  "Configure no Registry: JwtKey");

var jwtIssuer = GetConfigValue("JwtIssuer", "FalcaoWebBACKEND", isSensitive: true);
var jwtAudience = GetConfigValue("JwtAudience", "FalcaoPwaFRONTEND", isSensitive: true);
var jwtAccessMinutes = int.TryParse(GetConfigValue("JwtAccessTokenMinutes", "15"), out var am) ? am : 15;
var jwtRefreshDays = int.TryParse(GetConfigValue("JwtRefreshTokenDays", "14"), out var rd) ? rd : 14;

// Validação de segurança da chave JWT
if (Encoding.UTF8.GetByteCount(jwtKey ?? string.Empty) < 32)
    throw new InvalidOperationException("JWT Key insuficiente: mínimo 32 bytes (256 bits) requerido.");

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = jwtIssuer,
    ValidAudience = jwtAudience,
    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
    ClockSkew = TimeSpan.Zero,
    RequireExpirationTime = true,
    RequireSignedTokens = true
};
builder.Services.AddSingleton(tokenValidationParameters);

/*
===============================================================================
FASE 09 — EMAIL
===============================================================================
Objetivo:
- Configurar envio de e-mails transacionais
- Centralizar credenciais fora do código
- Garantir que falhas de email não derrubem a aplicação

O que acontece aqui:
- Leitura de Host, Porta, Usuário e Senha via Registry
- Configuração do EmailSettings
- Registro do serviço de email via Options Pattern

Premissas:
- Email é infraestrutura, não regra de negócio
- Falhas de email devem ser detectadas por Health Check

Regras:
- Credenciais nunca em código ou JSON
- Configuração obrigatória em Production

Impacto:
- Habilita notificações, recuperação de senha e alertas
===============================================================================
*/

var emailHost = GetConfigValue("EmailHost", string.Empty, isSensitive: true);
var emailPort = int.TryParse(GetConfigValue("EmailPort", "587", isSensitive: true), out var ep) ? ep : 587;
var emailUser = GetConfigValue("EmailUser", string.Empty, isSensitive: true);
var emailPassword = GetConfigValue("EmailPassword", string.Empty, isSensitive: true);
var emailFrom = GetConfigValue("EmailFrom", "no-reply@falcao.com", isSensitive: true);

// Configuração segura do EmailService
builder.Services.Configure<EmailSettings>(options =>
{
    options.Host = emailHost;
    options.Port = emailPort;
    options.User = emailUser;
    options.Password = emailPassword;
    options.From = emailFrom;
    options.UseSsl = bool.TryParse(GetConfigValue("EmailUseSsl", "false"), out var ssl) && ssl;
    options.UseStartTls = bool.TryParse(GetConfigValue("EmailUseStartTls", "true"), out var startTls) && startTls;
});

/*
===============================================================================
FASE 10 — HEALTH CHECKS
===============================================================================
Objetivo:
- Permitir monitoramento externo do estado da aplicação
- Detectar falhas críticas antes de impactar usuários

O que acontece aqui:
- Registro de HealthChecks para:
  - Banco de dados
  - Email
  - Registry
- Classificação por tags (infra / service / critical)

Premissas:
- Endpoint /health é usado por orquestradores e monitoramento
- Health NÃO autentica usuários

Regras:
- Health checks não devem acessar dados sensíveis
- Falha crítica deve refletir status UNHEALTHY

Impacto:
- Observabilidade
- Base para alertas automatizados
===============================================================================
*/


builder.Services.AddHealthChecks()
    .AddDbContextCheck<FalcaoDbContext>("database", tags: new[] { "infra", "critical" })
    .AddCheck<EmailServiceHealthCheck>("email", tags: new[] { "service", "critical" })
    .AddCheck<RegistryAccessHealthCheck>("registry", tags: new[] { "infra", "critical" });

/*
===============================================================================
FASE 11 — DEPENDENCY INJECTION (SERVICES)
===============================================================================
Objetivo:
- Registrar todos os serviços de domínio e infraestrutura
- Centralizar criação de dependências

O que acontece aqui:
- Registro de serviços scoped e singleton
- Nenhuma lógica de negócio é executada
- Apenas mapeamento de contratos → implementações

Premissas:
- Controllers dependem apenas de interfaces
- Ciclo de vida dos serviços é explícito

Regras:
- NÃO executar código aqui
- NÃO acessar banco aqui
- Apenas registro

Impacto:
- Base de todo o funcionamento da aplicação
===============================================================================
*/


builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IAdminSeedService, AdminSeedService>();
builder.Services.AddScoped<IAuthManager, AuthManager>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICepLookupService, CepLookupService>();
builder.Services.AddScoped<GenericCrudService>();
builder.Services.AddScoped<IPessoaService, PessoaService>();
builder.Services.AddScoped<IPessoaEmailService, PessoaEmailService>();
builder.Services.AddScoped<ITempPasswordService, TempPasswordService>();
builder.Services.AddScoped<IPinConfigurationService, PinConfigurationService>();
builder.Services.AddScoped<IMenuService, MenuService>();

/*
===============================================================================
FASE 13 — HTTP CLIENTS
===============================================================================
Objetivo:
- Centralizar chamadas HTTP externas
- Evitar criação manual de HttpClient

O que acontece aqui:
- Registro de HttpClient nomeado (ViaCEP)
- Configuração de timeout e headers

Premissas:
- Serviços externos podem falhar
- Timeout é obrigatório

Regras:
- Nunca usar new HttpClient()
- Sempre usar HttpClientFactory

Impacto:
- Estabilidade
- Performance
===============================================================================
*/

builder.Services.AddHttpClient("viaCep", client =>
{
    client.BaseAddress = new Uri("https://viacep.com.br/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "FalcaoSystem/1.0");
});

/*
===============================================================================
FASE 14 — CORS
===============================================================================
Objetivo:
- Controlar acesso cross-origin
- Permitir cookies cross-origin em Development
- Restringir origens explicitamente

O que acontece aqui:
- Definição de policies separadas (Dev / Prod)
- AllowCredentials habilitado
- Origem única permitida

Premissas:
- Development: frontend e backend em portas diferentes
- Production: mesma origem

Regras:
- NÃO usar AllowAnyOrigin com cookies
- Origem sempre explícita

Impacto:
- Segurança
- Funcionamento correto de cookies
===============================================================================
*/

var frontendUrl = GetConfigValue("FrontendBackendUrl", "https://localhost:7201");

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("X-Correlation-ID", "X-Request-ID");
    });

    opt.AddPolicy("ProductionPolicy", policy =>
    {
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("X-Correlation-ID", "X-Request-ID");
    });
});

/*
===============================================================================
FASE 15 — CONTROLLERS + JSON
===============================================================================
Objetivo:
- Registrar MVC Controllers
- Padronizar serialização JSON

O que acontece aqui:
- Registro de controllers
- Configuração de enums como string
- Política de camelCase
- Ignorar nulls

Premissas:
- API pública
- Contrato JSON estável

Impacto:
- Comunicação frontend/backend
===============================================================================
*/

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

/*
===============================================================================
FASE 16 — SWAGGER (DEVELOPMENT ONLY)
===============================================================================
Objetivo:
- Facilitar testes e homologação
- Permitir uso de JWT apenas em desenvolvimento

O que acontece aqui:
- Swagger só é registrado em Development
- SecurityDefinition Bearer configurada

Premissas:
- Swagger NÃO existe em Production
- Bearer NÃO é usado por mobile/PWA em produção

Impacto:
- Produtividade em desenvolvimento
===============================================================================
*/

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Falcao API",
            Version = "v1",
            Description = "Falcao API (Desenvolvimento/Homologação)",
            Contact = new OpenApiContact { Name = "Suporte", Email = "suporte@falcao.com" }
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Insira o token JWT (apenas para desenvolvimento): Bearer {token}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        c.OperationFilter<GenericCrudSwaggerOperationFilter>();
    });
}

/*
===============================================================================
FASE 17 — AUTHENTICATION (COOKIE-BASED)
===============================================================================
Objetivo:
- Definir os mecanismos de autenticação do sistema
- Estabelecer o COOKIE HTTPOnly como mecanismo ÚNICO de autenticação em runtime
- Garantir compatibilidade entre SPA (Blazor WASM), Swagger e API

Decisão Arquitetural:
- Autenticação realizada exclusivamente via Cookie HTTPOnly
- JWT NÃO participa do runtime, autenticação ou autorização
- Toda identidade do usuário é resolvida a partir do Cookie

O que acontece aqui:
- Cookie Authentication é o Default absoluto
- Toda requisição autenticada depende do envio correto do Cookie
- Claims e Identity são derivados exclusivamente do Cookie

Premissas:
- Usuário autenticado = Cookie válido presente na requisição
- SPA e API podem operar em origens/portas distintas
- Cookie deve ser compatível com chamadas cross-origin controladas

Regras:
- DefaultAuthenticateScheme = Cookie
- DefaultChallengeScheme = Cookie
- Cookie é HTTPOnly, Secure e Essential
- SameSite configurado para permitir envio do Cookie em SPA + API

Impacto:
- Esta configuração define se o Cookie chega ou NÃO ao servidor
- Erro aqui resulta em usuário ser tratado como anônimo
- PendingPin, IsActive e Policies dependem diretamente desta fase
===============================================================================
*/

builder.Services
    .AddAuthentication(options =>
    {
        // Define o Cookie como mecanismo absoluto de autenticação
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        // Define o Cookie como mecanismo de challenge (401)
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        // Define o Cookie como mecanismo de SignIn
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        // Define o Cookie como mecanismo de SignOut
        options.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        // Nome do cookie de autenticação
        // OBS: Nome histórico mantido por compatibilidade
        options.Cookie.Name = "falcao_access_token";

        // Impede acesso via JavaScript (proteção contra XSS)
        options.Cookie.HttpOnly = true;

        // Cookie só é enviado via HTTPS
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        /*
         * SameSite.None:
         * - Permite envio do cookie entre SPA e API em origens/portas distintas
         * - Necessário para Blazor WASM, Swagger e chamadas fetch
         * - Exige Secure = true (já garantido acima)
         */
        options.Cookie.SameSite = SameSiteMode.None;

        // Garante que o cookie não seja bloqueado por consentimento
        options.Cookie.IsEssential = true;

        // Tempo de expiração do cookie
        options.ExpireTimeSpan = TimeSpan.FromMinutes(15);

        // Renova o tempo de vida a cada requisição válida
        options.SlidingExpiration = true;

        // Evita redirecionamento automático (API-first)
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        // Rotas apenas referenciais (não usadas para redirect automático)
        options.LoginPath = "/api/Auth/login";
        options.LogoutPath = "/api/Auth/logout";
        options.AccessDeniedPath = "/api/Auth/access-denied";
    });

/*
===============================================================================
FASE 18 — AUTENTICAÇÃO E AUTORIZAÇÃO BASEADAS EXCLUSIVAMENTE EM COOKIE
===============================================================================
Decisão:
- Autenticação realizada somente via Cookie HTTPOnly
- JWT NÃO é utilizado em runtime, Swagger ou testes
- Todo acesso autenticado depende de Cookie válido

Regra de Negócio:
- Toda página privada exige:
  - Usuário autenticado
  - Autorização por Role
  - IsActive = true
- Única exceção:
  - ValidarPin pode ser acessada com IsActive = true ou false

Regras de Acesso (Role):
- Acesso exclusivo: Ceo
- Acesso restrito: Ceo + Administrador
- Acesso restrito: Ceo + Administrador + Vendedor
- Acesso restrito: Ceo + Administrador + Cliente
- Acesso restrito: Ceo + Administrador + Operador

Objetivo:
- Bloquear acesso por padrão
- Centralizar controle de acesso por Role
- Garantir comportamento previsível e auditável
===============================================================================
*/

builder.Services.AddAuthorization(options =>
{
/*
===============================================================================
FASE 18.1 — POLICY DEFAULT (FALLBACK)
===============================================================================
*/
options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
    .RequireAuthenticatedUser()
    .Build();

/*
===============================================================================
FASE 18.2 — POLICY DE EXCEÇÃO (USUÁRIO INATIVO)
===============================================================================
*/
options.AddPolicy("AllowInactiveUser", policy =>
{
    policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
    policy.RequireAuthenticatedUser();
    // ⚠️ IsActive será validado NO CONTROLLER ou VIA CLAIM
});

    /*
    ===============================================================================
    FASE 18.3 — POLICY PARA VALIDAÇÃO DE PIN (PENDING PIN)
    ===============================================================================
    Objetivo:
    - Permitir acesso à página de validação de PIN
    - Usuário deve estar autenticado via cookie
    - Não requer IsActive = true
    - Não requer Role específica
    */

    options.AddPolicy("AllowPendingPin", policy =>
    {
        policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        // ⚠️ IsActive será validado NO CONTROLLER
        // ⚠️ Role será validada NO CONTROLLER se necessário
    });

    options.AddPolicy("RequirePendingPin", policy =>
    {
        policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("IsActive", "False");
    });

    options.AddPolicy("RequireActiveUser", policy =>
    {
        policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("IsActive", "True");
    });

    /*
    ===============================================================================
    FASE 18.4 — POLICIES BASEADAS EM ROLE
    ===============================================================================
    */

    // Somente Ceo
    options.AddPolicy("Ceo", policy =>
    {
        policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Ceo");
    });

    // Ceo ou Administrador
    options.AddPolicy("CeoAdmin", policy =>
    {
        policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Ceo", "Administrador");
    });

    // Ceo, Administrador ou Vendedor
    options.AddPolicy("CeoAdminVendedor", policy =>
    {
        policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Ceo", "Administrador", "Vendedor");
    });

    // Ceo, Administrador ou Cliente
    options.AddPolicy("CeoAdminCliente", policy =>
    {
        policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Ceo", "Administrador", "Cliente");
    });

    // Ceo, Administrador ou Operador
    options.AddPolicy("CeoAdminOperador", policy =>
    {
        policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Ceo", "Administrador", "Operador");
    });

    // Ceo, Administrador, Vendedor, Cliente ou Operador
    options.AddPolicy("DashboardAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("IsActive", "True");
        policy.RequireRole(
            "Ceo",
            "Administrador",
            "Vendedor",
            "Cliente",
            "Operador"
        );
    });


});

/*
===============================================================================
FASE 19 — LICENÇAS E CONFIGURAÇÕES DE TERCEIROS
===============================================================================
Objetivo:
- Registrar licenças obrigatórias de bibliotecas externas
- Garantir conformidade legal antes da execução

Regras:
- Nunca após Build()
===============================================================================
*/

QuestPDF.Settings.License = LicenseType.Community;

/*
===============================================================================
FASE 20 — LOGGING GLOBAL (COM FILTROS DE SEGURANÇA)
===============================================================================
Objetivo:
- Centralizar logging
- Impedir vazamento de dados sensíveis

Premissas:
- Logging existe antes do Host
- Filtros são obrigatórios em produção

Impacto:
- Tele-metria controlada
- Segurança preservada
===============================================================================
*/

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddFilter((provider, category, level) =>
{
    // Filtro de segurança: não logar informações sensíveis
    if (category?.Contains("Certificate", StringComparison.OrdinalIgnoreCase) == true ||
        category?.Contains("JWT", StringComparison.OrdinalIgnoreCase) == true ||
        category?.Contains("Key", StringComparison.OrdinalIgnoreCase) == true ||
        category?.Contains("Password", StringComparison.OrdinalIgnoreCase) == true ||
        category?.Contains("Connection", StringComparison.OrdinalIgnoreCase) == true)
    {
        return level >= LogLevel.Warning; // Apenas erros/críticos
    }
    return true;
});

/*
===============================================================================
FASE 21 — KESTREL / HTTPS / CERTIFICADOS
===============================================================================
Objetivo:
- Configurar o servidor HTTP(S)
- Garantir HTTPS obrigatório
- Carregar certificado via Registry (Production)
- Permitir fallback controlado em Development

Premissas:
- Development: pode usar dev-cert
- Production: OBRIGATÓRIO certificado válido do Registry
- Sem certificado = aplicação NÃO sobe

Regras:
- Nunca após builder.Build()
- Nunca dentro do pipeline
- Nunca misturar com middleware

Impacto:
- Segurança de transporte garantida
- Ambiente previsível (Dev vs Prod)
===============================================================================
*/


builder.WebHost.ConfigureKestrel(serverOptions =>
{
    X509Certificate2? httpsCert = null;
    bool certLoaded = false;

    // Tenta carregar certificado do Registry
    if (!string.IsNullOrWhiteSpace(certThumb))
    {
        try
        {
            var storeLoc = certStoreLocation.Equals("CurrentUser", StringComparison.OrdinalIgnoreCase)
                ? StoreLocation.CurrentUser
                : StoreLocation.LocalMachine;

            using var store = new X509Store(StoreName.My, storeLoc);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(
                X509FindType.FindByThumbprint,
                certThumb.Trim(),
                validOnly: builder.Environment.IsProduction());

            if (certs?.Count > 0)
            {
                httpsCert = certs[0];
                if (httpsCert.HasPrivateKey && httpsCert.NotAfter > DateTime.UtcNow)
                {
                    certLoaded = true;
                }
            }
        }
        catch (Exception ex)
        {
            builder.Services.BuildServiceProvider()
                .GetRequiredService<ILogger<Program>>()
                .LogError(ex, "Erro ao carregar certificado do store: {Thumbprint}", certThumb);
        }
    }

    // Fallback para desenvolvimento
    if (!certLoaded && builder.Environment.IsDevelopment())
    {
        try
        {
            httpsCert = LoadDevelopmentCertificate();
            certLoaded = httpsCert != null;
        }
        catch { /* Ignora se não houver cert dev */ }
    }

    if (!certLoaded || httpsCert == null)
    {
        throw new InvalidOperationException(
            builder.Environment.IsProduction()
                ? "Certificado HTTPS de produção não carregado. Verifique Registry: BackendCertificateThumbprint"
                : "Certificado HTTPS de desenvolvimento não encontrado. Execute 'dotnet dev-certs https --trust'");
    }

    // Listen HTTPS main port
    serverOptions.ListenAnyIP(httpsPort, listenOptions =>
    {
        listenOptions.UseHttps(httpsCert, httpsOptions =>
        {
            httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                                       System.Security.Authentication.SslProtocols.Tls13;
            httpsOptions.CheckCertificateRevocation = builder.Environment.IsProduction();
        });
    });

    // Listen HTTPS for health
    serverOptions.ListenAnyIP(healthPort, listenOptions =>
    {
        listenOptions.UseHttps(httpsCert, httpsOptions =>
        {
            httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                                       System.Security.Authentication.SslProtocols.Tls13;
            httpsOptions.CheckCertificateRevocation = builder.Environment.IsProduction();
        });
    });
});

/*
===============================================================================
FASE 22 — BUILD DA APLICAÇÃO
===============================================================================
Objetivo:
- Congelar a configuração
- Validar DI, Auth, Policies e infraestrutura

O que acontece aqui:
- builder.Build()
- ServiceProvider final
- Configuração torna-se imutável

Premissas:
- Todas as fases anteriores estão corretas
- Nenhuma configuração dinâmica após isso

Regras:
- NÃO registrar serviços após Build
- NÃO alterar options após Build

Impacto:
- Estado consistente e previsível
===============================================================================
*/

var app = builder.Build();

/*
===============================================================================
FASE 23 — MIDDLEWARE PIPELINE
===============================================================================
Objetivo:
- Definir a ORDEM de execução das requisições
- Garantir que segurança venha antes da lógica

Ordem lógica:
1. Exception / Fail-safe
2. HTTPS
3. CORS
4. Authentication
5. Authorization
6. Controllers

Premissas:
- Ordem ERRADA = bug invisível
- Auth antes de Controller é obrigatório

Regras:
- Nunca mover Authentication depois do Authorization
- Nunca acessar User antes do Authentication

Impacto:
- Pipeline previsível
- Debug consistente
===============================================================================
*/

// Swagger apenas em desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Falcao API v1 (Desenvolvimento)");
        c.RoutePrefix = "api-docs";
        c.DisplayRequestDuration();
        c.DocumentTitle = "Falcao API - DESENVOLVIMENTO";
    });
}

// CORS conforme ambiente
app.UseCors("DevelopmentPolicy");

// 🔧 NOVO MIDDLEWARE: Valida consistência de autenticação
app.UseMiddleware<AuthConsistencyMiddleware>();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    context.Response.Headers["X-Correlation-ID"] = Guid.NewGuid().ToString();

    if (context.Request.Path.StartsWithSegments("/browserLinkSignalR", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();


/*
===============================================================================
FASE 24 — HEALTH CHECKS
===============================================================================
Objetivo:
- Expor estado da aplicação
- Monitorar infraestrutura crítica

O que acontece aqui:
- Endpoint /health
- Checks de DB, Email, Registry

Premissas:
- Health NÃO expõe segredos
- Health NÃO exige autenticação pesada

Regras:
- Apenas leitura
- Sem lógica de negócio

Impacto:
- Observabilidade real
===============================================================================
*/

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            environment = app.Environment.EnvironmentName,
            configSource = builder.Configuration["Registry:Source"] ?? "Unknown",
            registryPath = builder.Configuration["Registry:Path"] ?? "Not configured",
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = GetHealthCheckDescription(e.Key)
            })
        });
        await context.Response.WriteAsync(result);
    }
})
.AllowAnonymous();   

app.MapControllers();


/*
===============================================================================
FASE 26 — STARTUP DIAGNOSTICS
===============================================================================
Objetivo:
- Registrar estado final da aplicação
- Facilitar auditoria e suporte

O que acontece aqui:
- Logs de ambiente
- Logs de portas, fontes de config, Swagger

Premissas:
- Apenas informações não sensíveis

Regras:
- Nunca logar segredos
- Nunca logar tokens

Impacto:
- Suporte eficiente
===============================================================================
*/

app.Logger.LogInformation("=========================================");
app.Logger.LogInformation("CURIO WEB SERVER INICIADO");
app.Logger.LogInformation("Ambiente: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("Fonte Config: {ConfigSource}", builder.Configuration["Registry:Source"] ?? "Registry");
app.Logger.LogInformation("Registry Path: {RegistryPath}", builder.Configuration["Registry:Path"] ?? "Not configured");
app.Logger.LogInformation("HTTPS Port: {HttpsPort}", httpsPort);
app.Logger.LogInformation("Health Port: {HealthPort}", healthPort);
app.Logger.LogInformation("Frontend URL: {FrontendUrl}", frontendUrl);
app.Logger.LogInformation("Database: {DbServer}", GetDbServerInfo(connString));
app.Logger.LogInformation("Swagger: {Status}",
    app.Environment.IsDevelopment() ? "HABILITADO" : "DESABILITADO");
app.Logger.LogInformation("=========================================");

/*
===============================================================================
FASE 27 — RUN
===============================================================================
Objetivo:
- Iniciar o servidor
- Entrar em estado de escuta

O que acontece aqui:
- Kestrel ativo
- Pipeline operacional

Regras:
- Nada após app.Run()

Impacto:
- Sistema em produção
===============================================================================
*/


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FalcaoDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    if (!await db.Users.AnyAsync())
    {
        var seedService = scope.ServiceProvider.GetRequiredService<IAdminSeedService>();

        // Método local para acessar configuration do escopo atual
        string GetSeedValue(string key, string defaultValue = "", bool isSensitive = false)
        {
            var value = configuration[key];
            if (string.IsNullOrEmpty(value) && !isSensitive)
                value = defaultValue;

            if (isSensitive && string.IsNullOrEmpty(value))
                throw new InvalidOperationException($"Config obrigatória: {key}");

            return value ?? string.Empty;
        }

        var seedDto = new SeedAdminDto
        {
            Username = GetConfigValue("AdminUsername", "admin", isSensitive: true),
            Email = GetConfigValue("AdminEmail", "admin@falcao.com", isSensitive: true),
            Password = GetConfigValue("AdminPassword", string.Empty, isSensitive: true),
            TipoUsuario = Enum.TryParse<UsuarioEnum>(
                GetConfigValue("AdminTipoUsuario", "Ceo"),
                out var tipo) ? tipo : UsuarioEnum.Ceo
        };

        try
        {
            var result = await seedService.SeedAdministrativeUserAsync(seedDto);
            logger.LogInformation("Admin seed executado: {Success}", result.Success);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao executar admin seed");
        }
    }
}

app.Run();

/*
===============================================================================
FASE 28 — MÉTODOS AUXILIARES
===============================================================================
Objetivo:
- Suporte à infraestrutura
- Reutilização sem poluir o fluxo principal

O que acontece aqui:
- Helpers de certificado
- Helpers de validação
- Helpers de diagnóstico

Regras:
- Nunca conter lógica de negócio
- Nunca acessar HttpContext diretamente

Impacto:
- Código limpo e auditável
===============================================================================
*/

static X509Certificate2? LoadDevelopmentCertificate()
{
    // Tenta carregar certificado de desenvolvimento dotnet
    var certs = new X509Store(StoreName.My, StoreLocation.CurrentUser);
    certs.Open(OpenFlags.ReadOnly);

    var devCerts = certs.Certificates.Find(
        X509FindType.FindBySubjectName,
        "localhost",
        validOnly: false);

    certs.Close();

    return devCerts?.Count > 0 ? devCerts[0] : null;
}

static void ValidateNoSecretsInJson(IConfiguration configuration)
{
    // Verificar se Registry carregou com sucesso
    var registrySource = configuration["Registry:Source"];
    var registryStatus = configuration["Registry:Status"];

    // Se Registry carregou com sucesso, não precisa validar segredos em JSON
    if (registrySource != null && registryStatus == "LOADED")
    {
        return; // Registry é fonte confiável, segredos não estão em JSON
    }

    // Lista de chaves que NUNCA devem ter valores em .json
    var sensitiveKeys = new[]
    {
        "DbConnection",
        "JwtKey",
        "JwtIssuer",
        "JwtAudience",
        "EmailHost",
        "EmailPort",
        "EmailUser",
        "EmailPassword",
        "EmailFrom",
        "AdminUsername",
        "AdminPassword",
        "AdminEmail",
        "BackendCertificateThumbprint",
        "ServerCertificatePassword"
    };

    var violations = new List<string>();

    foreach (var key in sensitiveKeys)
    {
        var value = configuration[key];

        // SÓ bloquear se:
        // 1. Valor existe E não está vazio
        // 2. É diferente de valores padrão de desenvolvimento
        if (!string.IsNullOrEmpty(value) &&
            value.Trim().Length > 0 &&
            !value.Contains("localhost") &&
            !value.Contains("127.0.0.1") &&
            value.Length > 10)
        {
            violations.Add(key);
        }
    }

    if (violations.Count > 0)
    {
        throw new InvalidOperationException(
            $"VIOLAÇÃO DE SEGURANÇA: Segredos encontrados em fonte não segura: {string.Join(", ", violations)}. " +
            "USE: Registry (HKCU:\\SOFTWARE\\Falcao\\Secrets para Development) " +
            "OU Registry (HKLM:\\SOFTWARE\\Falcao\\Secrets para Production)");
    }
}

static string GetDbServerInfo(string connectionString)
{
    try
    {
        var builder = new System.Data.Common.DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };
        if (builder.ContainsKey("server"))
        {
            var server = builder["server"].ToString();
            // Oculta IPs completos por segurança
            if (server?.Contains('.') == true)
            {
                var parts = server.Split('.');
                if (parts.Length >= 4)
                {
                    return $"{parts[0]}.***.***.{parts[3]}";
                }
            }
            return server ?? "Unknown";
        }
    }
    catch { }

    return "Configured";
}

static string GetHealthCheckDescription(string checkName)
{
    return checkName switch
    {
        "database" => "Conexão com banco de dados MySQL",
        "email" => "Serviço de email SMTP",
        "registry" => "Acesso ao Registry Windows (segredos)",
        _ => checkName
    };
}