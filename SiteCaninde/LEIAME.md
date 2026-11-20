# Solicitações de associação no FalcaoPwaSolution (RASCUNHO NÃO COMPILADO)

Estes 3 arquivos foram escritos sem acesso a compilador .NET e sem ver `FalcaoDbContext`,
`AuthConsistencyMiddleware` nem `IEmailService`. Trate como ponto de partida e compile antes de qualquer coisa.

## Onde colocar
- `SolicitacaoAssociacao.cs` → FalcaoWebServer/Models/Associacao
- `AssociacaoDto.cs` → FalcaoPwaShared/Models/Dto
- `PublicoAssociacaoController.cs` → FalcaoWebServer/Controllers

## Passos
1. No DbContext: `public DbSet<SolicitacaoAssociacao> SolicitacoesAssociacao => Set<SolicitacaoAssociacao>();`
2. `dotnet ef migrations add SolicitacaoAssociacao` e `dotnet ef database update` (só no banco de desenvolvimento, com dados FICTÍCIOS).
3. Program.cs (respeite a ordem do seu checklist):
```csharp
// serviços
var origens = (builder.Configuration["SiteAssociacaoOrigins"]
    ?? "https://www.aafrimisa.com.br,https://aafrimisa.com.br").Split(',', StringSplitOptions.RemoveEmptyEntries);
builder.Services.AddCors(o => o.AddPolicy("SiteAssociacao", p => p
    .WithOrigins(origens).WithMethods("POST").WithHeaders("Content-Type"))); // sem credenciais

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("solicitacao", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
});
builder.Services.Configure<ForwardedHeadersOptions>(o =>
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

// pipeline
app.UseForwardedHeaders();   // no início
// ... UseCors(...) existente ...
app.UseRateLimiter();        // logo depois do UseCors
```
Usings: `System.Threading.RateLimiting`, `Microsoft.AspNetCore.RateLimiting`, `Microsoft.AspNetCore.HttpOverrides`.
Se o túnel/proxy não repassar `X-Forwarded-For`, todos os visitantes caem no mesmo limite (5 por 10 min no total).

## Testes (PowerShell, dados fictícios; CPF de teste válido 529.982.247-25)
```powershell
# preflight CORS
curl.exe -i -X OPTIONS https://SEU-HOST/api/publico/associacao/solicitacoes `
  -H "Origin: https://www.aafrimisa.com.br" -H "Access-Control-Request-Method: POST" -H "Access-Control-Request-Headers: content-type"
# envio
curl.exe -i -X POST https://SEU-HOST/api/publico/associacao/solicitacoes -H "Origin: https://www.aafrimisa.com.br" `
  -H "Content-Type: application/json" -d '{"nome":"Maria Teste","dataNascimento":"1990-05-10","cpf":"52998224725","telefone":"31999999999","aceiteDeclaracao":true,"aceiteLgpd":true,"assinatura":"maria teste","canais":[]}'
```
Esperado: preflight 204 com `Access-Control-Allow-Origin`; POST 201 com `protocolo`. Teste também origem não permitida,
CPF inválido (400) e 6 envios seguidos (429). Confirme que o `AuthConsistencyMiddleware` não barra chamadas anônimas.

## Suposições
Namespaces e nome do DbContext; chaves de configuração `AssociacaoEntidade` e `SiteAssociacaoOrigins`;
`DateTime` (coluna date) em vez de `DateOnly` na entidade por incerteza do provedor MySQL; sem vínculo com a entidade Pessoa ainda;
sem bloqueio de CPF duplicado (evita enumerar CPFs; sinalizar duplicidade no painel).

## Pendências antes de dados reais / nuvem
- Criptografar CPF/RG em repouso e guardar hash do CPF para detectar duplicidade.
- Rodar em Production (em Development o EF pode registrar parâmetros SQL com CPF).
- Turnstile (captcha) no formulário; painel da Diretoria (deferir/indeferir, categoria, parecer) com policy `CeoAdmin`.
- Fase final: API envia o e-mail via `IEmailService` e o Formspree é desligado (`USAR_FORMSPREE = false`).
- Domínio público HTTPS da API com certificado de CA válida (ex.: api.aafrimisa.com.br).

## Ao ativar a API no site
Preencha `API_URL` no index.html e acrescente na Política de Privacidade: os dados passam a ser gravados também em
banco de dados da associação (informar onde fica hospedado) e o prazo de guarda.
