using Blazored.Toast;
using CanindePwaClient;
using CanindePwaClient.Auth;
using CanindePwaClient.Handlers;
using CanindePwaClient.Services;
using CanindePwaClient.Services.Menu;
using CanindePwaClient.Services.Offline;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ======================
// 1. RESOLUCAO DO BACKEND (SEGURA)
// ======================
var backendUrl = ResolveBackendUrl();

// VALIDACAO HTTPS ABSOLUTA
if (!backendUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        $"VIOLACAO DE SEGURANCA: Backend deve ser HTTPS. URL: {backendUrl}");
}

// ======================
// 2. REGISTRO DOS HANDLERS
// ======================
builder.Services.AddScoped<CookieHandler>();
builder.Services.AddScoped<SecurityHeadersHandler>();
builder.Services.AddScoped<WasmRetryHandler>();
builder.Services.AddScoped<WasmCircuitBreakerHandler>();

// ======================
// 3. OUTROS SERVICOS
// ======================
builder.Services.AddScoped<NavigationService>();
builder.Services.AddScoped<UserContext>();
builder.Services.AddScoped<IClientMenuService, ClientMenuService>();

// ======================
// 4. HTTP CLIENT CONFIGURADO (Handlers em Cascata)
// ======================
builder.Services.AddHttpClient("CanindeApi", client =>
{
    client.BaseAddress = new Uri(backendUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "CanindePwaClient/1.0");
    client.DefaultRequestHeaders.Add("X-CANINDE-Client", "Blazor-WASM");
    client.DefaultRequestHeaders.Add("X-CANINDE-Version", "1.0.0");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<CookieHandler>()           // 1º: Cookies HTTPOnly
.AddHttpMessageHandler<SecurityHeadersHandler>()  // 2º: Headers OWASP
.AddHttpMessageHandler<WasmRetryHandler>()        // 3º: Retry Inteligente
.AddHttpMessageHandler<WasmCircuitBreakerHandler>(); // 4º: Circuit Breaker

// ======================
// 5. SERVICOS DE APLICACAO
// ======================
builder.Services.AddScoped<IApiService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("CanindeApi");
    return new ApiService(client);
});

// ======================
// 6. AUTENTICACAO (COOKIES HTTPOnly)
// ======================
builder.Services.AddScoped<CookieAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CookieAuthStateProvider>());
builder.Services.AddScoped<AuthManager>();

// ======================
// 7. OFFLINE (MODO FAZENDA)
// ======================
builder.Services.AddScoped<IOfflineCacheService, OfflineCacheService>();
builder.Services.AddScoped<IOfflineSyncService, OfflineSyncService>();

// ======================
// 8. UTILITARIOS
// ======================
builder.Services.AddBlazoredToast();
builder.Services.AddAuthorizationCore(); // 🔴 APENAS ISSO - SEM POLICIES

builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNameCaseInsensitive = true;
    options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    options.Converters.Add(new JsonStringEnumConverter());
});

// ============================================================================
// 9. AUTORIZAÇÃO (FRONTEND)
// ============================================================================
// - Estas policies NÃO substituem validação do backend
// - NÃO garantem segurança real
// - Servem para:
//   • Controle de navegação
//   • Habilitar/desabilitar páginas no Razor
//   • Representar ESTADOS do usuário no client
// ============================================================================
builder.Services.AddAuthorizationCore(options =>
{
    // =====================================================
    // ESTADOS DE AUTENTICAÇÃO
    // =====================================================

    // Usuário autenticado (qualquer estado)
    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());

    // Usuário ativo (Estado 1)
    options.AddPolicy("RequireActiveUser", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("IsActive", "True");
    });

    // Usuário pendente de PIN (Estado 2)
    options.AddPolicy("RequirePendingPin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("IsActive", "False");
    });

    // =====================================================
    // ROLES (frontend = sem regra real)
    // =====================================================

    options.AddPolicy("Ceo", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("CeoAdmin", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("CeoAdminOperador", policy =>
        policy.RequireAuthenticatedUser());
});

// ======================
// 10. INICIALIZACAO
// ======================
await builder.Build().RunAsync();

// ======================
// FUNCOES AUXILIARES
// ======================

static string ResolveBackendUrl()
{
    var backendUrl = Environment.GetEnvironmentVariable("CANINDE__ENVIRONMENT");
    if (!string.IsNullOrEmpty(backendUrl))
    {
        return EnsureHttps(backendUrl);
    }

    return "https://localhost:7199";
}

static string EnsureHttps(string url)
{
    if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
    {
        return url.Replace("http://", "https://");
    }

    if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
        return $"https://{url}";
    }

    return url;
}

