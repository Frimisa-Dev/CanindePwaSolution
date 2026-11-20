# CanindePwaSolution - Estrutura e Auditoria

> Gerado em 2026-11-13 14:32:41
> READ-ONLY. Nenhum segredo lido. Nenhum valor de Registry exposto.


## 1. Sumario Executivo

| Metrica | Valor |
|---|---|
| Projetos | 3 |
| DTOs totais | 45 |
| DTOs aderentes | 24 |
| DTOs a renomear | 1 |
| Endpoints | 37 |
| Publicos (AllowAnonymous) | 18 |
| DbSets | 33 |
| Servicos DI | 11 |
| Enums | 10 |
| Politicas | 8 |
| Flags seguranca | 57 |
| Secoes com falha | 0 |


## 2. Arvore de Diretorios

```
CanindePwaSolution/
├── .netlify/
│   ├── functions-internal/
│   ├── v1/
│   │   └── functions/
│   └── netlify.toml
├── CanindePwaClient/
│   ├── Auth/
│   │   ├── AuthManager.cs
│   │   ├── CookieAuthStateProvider.cs
│   │   ├── CookieAuthStateProvider.cs.backup
│   │   └── UserContext.cs
│   ├── Components/
│   │   ├── BackButton.razor
│   │   ├── MensagemPadrao.razor
│   │   └── NetworkStatus.razor
│   ├── Handlers/
│   │   ├── CookieHandler.cs
│   │   ├── SecurityHeadersHandler.cs
│   │   ├── WasmCircuitBreakerHandler.cs
│   │   └── WasmRetryHandler.cs
│   ├── Pages/
│   │   ├── Auth/
│   │   │   ├── CadastrarUsuario.razor
│   │   │   ├── Dashboard.razor
│   │   │   └── ValidarPin.razor
│   │   ├── Home.razor
│   │   ├── Login.razor
│   │   └── RecuperarAcesso.razor
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Services/
│   │   ├── Offline/
│   │   │   ├── IOfflineCacheService.cs
│   │   │   ├── IOfflineSyncService.cs
│   │   │   ├── OfflineCacheService.cs
│   │   │   └── OfflineSyncService.cs
│   │   ├── ApiService.cs
│   │   ├── ClientMenuService.cs
│   │   ├── IApiService.cs
│   │   ├── IClientMenuService.cs
│   │   ├── NavigationService.cs
│   │   └── SecurityUtilities.cs
│   ├── Shared/
│   │   ├── DashboardLayout.razor
│   │   ├── MainLayout.razor
│   │   ├── RedirectToLogin.razor
│   │   └── SidebarDinamico.razor
│   ├── wwwroot/
│   │   ├── css/
│   │   │   └── app.css
│   │   ├── images/
│   │   │   ├── logo.png
│   │   │   └── marca.png
│   │   ├── lib/
│   │   │   └── bootstrap/
│   │   │       └── dist/
│   │   │           ├── css/
│   │   │           │   ├── bootstrap-grid.css
│   │   │           │   ├── bootstrap-grid.css.map
│   │   │           │   ├── bootstrap-grid.min.css
│   │   │           │   ├── bootstrap-grid.min.css.map
│   │   │           │   ├── bootstrap-grid.rtl.css
│   │   │           │   ├── bootstrap-grid.rtl.css.map
│   │   │           │   ├── bootstrap-grid.rtl.min.css
│   │   │           │   ├── bootstrap-grid.rtl.min.css.map
│   │   │           │   ├── bootstrap-reboot.css
│   │   │           │   ├── bootstrap-reboot.css.map
│   │   │           │   ├── bootstrap-reboot.min.css
│   │   │           │   ├── bootstrap-reboot.min.css.map
│   │   │           │   ├── bootstrap-reboot.rtl.css
│   │   │           │   ├── bootstrap-reboot.rtl.css.map
│   │   │           │   ├── bootstrap-reboot.rtl.min.css
│   │   │           │   ├── bootstrap-reboot.rtl.min.css.map
│   │   │           │   ├── bootstrap-utilities.css
│   │   │           │   ├── bootstrap-utilities.css.map
│   │   │           │   ├── bootstrap-utilities.min.css
│   │   │           │   ├── bootstrap-utilities.min.css.map
│   │   │           │   ├── bootstrap-utilities.rtl.css
│   │   │           │   ├── bootstrap-utilities.rtl.css.map
│   │   │           │   ├── bootstrap-utilities.rtl.min.css
│   │   │           │   ├── bootstrap-utilities.rtl.min.css.map
│   │   │           │   ├── bootstrap.css
│   │   │           │   ├── bootstrap.css.map
│   │   │           │   ├── bootstrap.min.css
│   │   │           │   ├── bootstrap.min.css.map
│   │   │           │   ├── bootstrap.rtl.css
│   │   │           │   ├── bootstrap.rtl.css.map
│   │   │           │   ├── bootstrap.rtl.min.css
│   │   │           │   └── bootstrap.rtl.min.css.map
│   │   │           └── js/
│   │   │               ├── bootstrap.bundle.js
│   │   │               ├── bootstrap.bundle.js.map
│   │   │               ├── bootstrap.bundle.min.js
│   │   │               ├── bootstrap.bundle.min.js.map
│   │   │               ├── bootstrap.esm.js
│   │   │               ├── bootstrap.esm.js.map
│   │   │               ├── bootstrap.esm.min.js
│   │   │               ├── bootstrap.esm.min.js.map
│   │   │               ├── bootstrap.js
│   │   │               ├── bootstrap.js.map
│   │   │               ├── bootstrap.min.js
│   │   │               └── bootstrap.min.js.map
│   │   ├── appsettings.json
│   │   ├── favicon.png
│   │   ├── icon-192.png
│   │   └── index.html
│   ├── _Imports.razor
│   ├── App.razor
│   ├── build_log.txt
│   ├── erroClient
│   ├── erroServer
│   ├── EstruturaFrontend
│   ├── CanindePwaClient.csproj
│   └── Program.cs
├── CanindePwaShared/
│   ├── Enums/
│   │   ├── AuthModeEnum.cs
│   │   ├── ClassificacaoSocioEnum.cs
│   │   ├── EstatusEnum.cs
│   │   ├── PerfilEnum.cs
│   │   ├── ResultadoEnum.cs
│   │   ├── SetorEnum.cs
│   │   ├── SimNaoEnum.cs
│   │   ├── TipoCepEnum.cs
│   │   ├── TipoMensagemEnum.cs
│   │   └── UsuarioEnum.cs
│   ├── Caninde.Api/
│   │   ├── appsettings.Development.json
│   │   └── appsettings.Production.json
│   ├── Models/
│   │   ├── Auth/
│   │   │   ├── EmailPin.cs
│   │   │   ├── EmailSettings.cs
│   │   │   ├── Menu.cs
│   │   │   ├── Perfil.cs
│   │   │   ├── RefreshToken.cs
│   │   │   ├── Role.cs
│   │   │   ├── Rota.cs
│   │   │   ├── SecurityPin.cs
│   │   │   ├── TempPassword.cs
│   │   │   ├── User.cs
│   │   │   └── UserRole.cs
│   │   ├── Dto/
│   │   │   ├── AuthDto/
│   │   │   │   ├── AuditLogDto.cs
│   │   │   │   ├── CadastroUsuarioDto.cs
│   │   │   │   ├── CadastroUsuarioResponseDto.cs
│   │   │   │   ├── ForgotPasswordDto.cs
│   │   │   │   ├── LoginDto.cs
│   │   │   │   ├── LoginResponseDto.cs
│   │   │   │   ├── MenuItemDto.cs
│   │   │   │   ├── MenuResponseDto.cs
│   │   │   │   ├── PasswordResetTokenDto.cs
│   │   │   │   ├── PendingPinDto.cs
│   │   │   │   ├── PerfilDto.cs
│   │   │   │   ├── PinStatusResponseDto.cs
│   │   │   │   ├── RefreshTokenResponseDto.cs
│   │   │   │   ├── ResendPinDto.cs
│   │   │   │   ├── ResetPasswordDto.cs
│   │   │   │   ├── RotaDto.cs
│   │   │   │   ├── SeedAdminDto.cs
│   │   │   │   ├── SeedResultDto.cs
│   │   │   │   ├── ValidatePinDto.cs
│   │   │   │   ├── VerifyPinDto.cs
│   │   │   │   └── VerifyPinRequestDto.cs
│   │   │   ├── CommonDto/
│   │   │   │   ├── AuthResponseDto.cs
│   │   │   │   ├── BoolWrapperDto.cs
│   │   │   │   ├── CachedItemDto.cs
│   │   │   │   ├── ChangePasswordDto.cs
│   │   │   │   ├── OfflineOperationDto.cs
│   │   │   │   └── OperationResponseDto.cs
│   │   │   ├── TableDto/
│   │   │   │   ├── BairroRequestDto.cs
│   │   │   │   ├── BairroResponseDto.cs
│   │   │   │   ├── PessoaEmailRequestDto.cs
│   │   │   │   ├── PessoaEmailResponseDto.cs
│   │   │   │   ├── PessoaRequestDto.cs
│   │   │   │   ├── PessoaResponseDto.cs
│   │   │   │   ├── PessoaSocioBenemeritoRequestDto.cs
│   │   │   │   ├── PessoaSocioBenemeritoResponseDto.cs
│   │   │   │   ├── PessoaSocioContribuinteRequestDto.cs
│   │   │   │   ├── PessoaSocioContribuinteResponseDto.cs
│   │   │   │   ├── PessoaSocioFundadorRequestDto.cs
│   │   │   │   ├── PessoaSocioFundadorResponseDto.cs
│   │   │   │   ├── PessoaSocioUsuarioRequestDto.cs
│   │   │   │   └── PessoaSocioUsuarioResponseDto.cs
│   │   │   └── UserDto/
│   │   │       ├── ChangePasswordRequestDto.cs
│   │   │       ├── UpdateUserDto.cs
│   │   │       ├── UserProfileDto.cs
│   │   │       └── UserProfileResponseDto.cs
│   │   ├── Tbl/
│   │   │   ├── MaxLengthAttribute.cs
│   │   │   ├── TblBairro.cs
│   │   │   ├── TblBase.cs
│   │   │   ├── TblCep.cs
│   │   │   ├── TblCidade.cs
│   │   │   ├── TblEstado.cs
│   │   │   ├── TblLogradouro.cs
│   │   │   ├── TblParametro.cs
│   │   │   ├── TblPessoa.cs
│   │   │   ├── TblPessoaEmail.cs
│   │   │   ├── TblPessoaFisica.cs
│   │   │   ├── TblPessoaJuridica.cs
│   │   │   ├── TblPessoaSocioBenemerito.cs
│   │   │   ├── TblPessoaSocioContribuinte.cs
│   │   │   ├── TblPessoaSocioFundador.cs
│   │   │   └── TblPessoaSocioUsuario.cs
│   │   └── Uvw/
│   │       ├── UvwBairro.cs
│   │       ├── UvwCep.cs
│   │       ├── UvwCidade.cs
│   │       ├── UvwEstado.cs
│   │       ├── UvwLogradouro.cs
│   │       ├── UvwParametro.cs
│   │       ├── UvwPessoa.cs
│   │       ├── UvwPessoaEmail.cs
│   │       ├── UvwPessoaFisica.cs
│   │       └── UvwPessoaJuridica.cs
│   ├── Documentacao Tecnica CurioPwaShared.txt
│   └── CanindePwaShared.csproj
├── CanindeWebServer/
│   ├── Backup/
│   ├── Configuration/
│   │   └── CanindeRegistryConfiguration.cs
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── GenericCrudController.cs
│   │   ├── MenuController.cs
│   │   ├── PessoaController.cs
│   │   ├── PessoaEmailController.cs
│   │   └── UsersController.cs
│   ├── Data/
│   │   └── CanindeDbContext.cs
│   ├── Middleware/
│   │   ├── Reports/
│   │   │   └── PedidoVenda/
│   │   │       └── 2025/
│   │   │           └── 11/
│   │   │               └── 005001.pdf
│   │   └── AuthConsistencyMiddleware.cs
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Repositories/
│   │   ├── IUserRepository.cs
│   │   └── UserRepository.cs
│   ├── Services/
│   │   ├── Health/
│   │   │   └── EmailServiceHealthCheck.cs
│   │   ├── Reports/
│   │   ├── Utils/
│   │   │   ├── CpfCnpjValidator.cs
│   │   │   ├── FormatUtils.cs
│   │   │   ├── PdfReportBuilder.cs
│   │   │   └── UtilizarEnum.cs
│   │   ├── AdminSeedService.cs
│   │   ├── AuthService.cs
│   │   ├── CepLookupService.cs
│   │   ├── EmailService.cs
│   │   ├── GenericCrudService.cs
│   │   ├── IAdminSeedService.cs
│   │   ├── IAuthService.cs
│   │   ├── ICepLookupService.cs
│   │   ├── IEmailService.cs
│   │   ├── IMenuService.cs
│   │   ├── IPasswordHasher.cs
│   │   ├── IPessoaEmailService.cs
│   │   ├── IPessoaService.cs
│   │   ├── IPinConfigurationService.cs
│   │   ├── ITempPasswordService.cs
│   │   ├── MenuService.cs
│   │   ├── PasswordHasherService.cs
│   │   ├── PessoaEmailService.cs
│   │   ├── PessoaService.cs
│   │   ├── PinConfigurationService.cs
│   │   └── TempPasswordService.cs
│   ├── Swagger/
│   │   └── GenericCrudSwaggerOperationFilter.cs
│   ├── appsettings.json
│   ├── cookies.txt
│   ├── Documentacao Tecnica CurioWebServer.txt
│   ├── erroClient
│   ├── erroServer
│   ├── CanindeWebServer.csproj
│   ├── Program.cs
│   └── startup-crash.log
├── PowerShell/
│   ├── Criar-Usuario-CanindeWeb.ps1
│   ├── Deploy-CanindeWebServer-Server.ps1
│   ├── Publish-CanindeWebServer-Local.ps1
│   ├── ScriptInfraNuvem.txt
│   └── Setup-CanindeDevBegin.ps1
├── SiteFrimisa/
│   ├── image/
│   │   ├── logo-slogan.png
│   │   ├── logo.png
│   │   ├── tela1.png
│   │   ├── tela2.png
│   │   ├── tela3.png
│   │   └── tela4.png
│   ├── estilo.css
│   ├── index.html
│   ├── LEIAME.md
│   ├── privacidade.html
│   ├── regimento-interno.pdf
│   └── termos.html
├── .gitignore
├── BackupSistema.bat
├── diff-temp.txt
├── Email - 5 contas - Zoho Mail configuracao Registro.br
├── Estrutura
├── CanindePwaSolution.sln
├── CanindePwaSolution.slnLaunch.user
├── ProjetoGeral.txt
├── publicar-site.ps1
├── README.md
└── Relatório Técnico.pdf
```


## 3. Projetos e Pacotes

### CanindePwaClient

| Campo | Valor |
|---|---|
| Caminho | `CanindePwaClient\CanindePwaClient.csproj` |
| SDK | `Microsoft.NET.Sdk.BlazorWebAssembly` |
| TFM | `net10.0` |
| Nullable | `enable` |

| Pacote | Versao |
|---|---|
| Blazored.LocalStorage | 4.5.0 |
| Blazored.Toast | 4.2.1 |
| Blazorise.Bootstrap5 | 1.8.7 |
| Blazorise.Charts | 1.8.7 |
| Blazorise.Icons.FontAwesome | 1.8.7 |
| Microsoft.AspNetCore.Components.WebAssembly | 10.0.13 |
| Microsoft.AspNetCore.Components.WebAssembly.Authentication | 10.0.13 |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer | 10.0.13 |
| Microsoft.Extensions.Http | 10.0.13 |
| Microsoft.Extensions.Http | 9.0.0 |
| System.Net.Http.Json | 10.0.13 |
| System.Net.Http.Json | 9.0.0 |

### CanindePwaShared

| Campo | Valor |
|---|---|
| Caminho | `CanindePwaShared\CanindePwaShared.csproj` |
| SDK | `Microsoft.NET.Sdk` |
| TFM | `net10.0` |
| Nullable | `enable` |

### CanindeWebServer

| Campo | Valor |
|---|---|
| Caminho | `CanindeWebServer\CanindeWebServer.csproj` |
| SDK | `Microsoft.NET.Sdk.Web` |
| TFM | `net10.0` |
| Nullable | `enable` |

| Pacote | Versao |
|---|---|
| AspNetCore.HealthChecks.UI.Client | 9.0.0 |
| BCrypt.Net-Next | 4.0.3 |
| MailKit | 4.15.1 |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.1 |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.1 |
| Microsoft.EntityFrameworkCore | 10.0.1 |
| Microsoft.EntityFrameworkCore.Design | 10.0.1 |
| Microsoft.EntityFrameworkCore.Relational | 10.0.1 |
| Microsoft.EntityFrameworkCore.Tools | 10.0.1 |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 10.0.1 |
| Microsoft.IdentityModel.Tokens | 8.0.1 |
| MySql.EntityFrameworkCore | 10.0.1 |
| QuestPDF | 2025.12.0 |
| Swashbuckle.AspNetCore | 9.0.6 |
| System.IdentityModel.Tokens.Jwt | 8.0.1 |
| System.Security.Cryptography.Pkcs | 10.0.1 |


## 4. Inventario de DTOs

| Arquivo | Classe | Status |
|---|---|---|
| `CanindePwaShared\Models\Dto\AuthDto\AuditLogDto.cs` | `AuditLogDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\AuthDto\CadastroUsuarioDto.cs` | `CadastroUsuarioDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\AuthDto\CadastroUsuarioResponseDto.cs` | `CadastroUsuarioResponseDto` | OK |
| `CanindePwaShared\Models\Dto\AuthDto\ForgotPasswordDto.cs` | `ForgotPasswordDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\AuthDto\LoginDto.cs` | `LoginDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\AuthDto\LoginResponseDto.cs` | `LoginResponseDto` | OK |
| `CanindePwaShared\Models\Dto\AuthDto\MenuItemDto.cs` | `MenuItemDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\AuthDto\MenuResponseDto.cs` | `MenuResponseDto` | OK |
| `CanindePwaShared\Models\Dto\AuthDto\PasswordResetTokenDto.cs` | `PasswordResetTokenDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\AuthDto\PendingPinDto.cs` | `PendingPinDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\AuthDto\PerfilDto.cs` | `PerfilDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\AuthDto\PinStatusResponseDto.cs` | `PinStatusResponseDto` | OK |
| `CanindePwaShared\Models\Dto\AuthDto\RefreshTokenResponseDto.cs` | `RefreshTokenResponseDto` | OK |
| `CanindePwaShared\Models\Dto\AuthDto\ResendPinDto.cs` | `ResendPinDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\AuthDto\ResetPasswordDto.cs` | `ResetPasswordDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\AuthDto\RotaDto.cs` | `RotaDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\AuthDto\SeedAdminDto.cs` | `SeedAdminDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\AuthDto\SeedResultDto.cs` | `SeedResultDto` | RENOMEAR -> *ResponseDto |
| `CanindePwaShared\Models\Dto\AuthDto\ValidatePinDto.cs` | `ValidatePinDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\AuthDto\VerifyPinDto.cs` | `VerifyPinDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\AuthDto\VerifyPinRequestDto.cs` | `VerifyPinResponseDto` | OK |
| `CanindePwaShared\Models\Dto\CommonDto\AuthResponseDto.cs` | `AuthResponseDto` | OK |
| `CanindePwaShared\Models\Dto\CommonDto\BoolWrapperDto.cs` | `BoolWrapperDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\CommonDto\CachedItemDto.cs` | `CachedItemDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\CommonDto\ChangePasswordDto.cs` | `ChangePasswordDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\CommonDto\OfflineOperationDto.cs` | `OfflineOperationDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\CommonDto\OperationResponseDto.cs` | `OperationResponseDto` | OK |
| `CanindePwaShared\Models\Dto\TableDto\BairroRequestDto.cs` | `TblBairroRequestDto` | OK |
| `CanindePwaShared\Models\Dto\TableDto\BairroResponseDto.cs` | `TblBairroResponseDto` | OK |
| `CanindePwaShared\Models\Dto\TableDto\PessoaEmailRequestDto.cs` | `TblPessoaEmailRequestDto` | OK |
| `CanindePwaShared\Models\Dto\TableDto\PessoaEmailResponseDto.cs` | `TblPessoaEmailResponseDto` | OK |
| `CanindePwaShared\Models\Dto\TableDto\PessoaRequestDto.cs` | `TblPessoaRequestDto` | OK |
| `CanindePwaShared\Models\Dto\TableDto\PessoaResponseDto.cs` | `TblPessoaResponseDto` | OK |
| `CanindePwaShared\Models\Dto\TableDto\PessoaSocioBenemeritoRequestDto.cs` | `TblPessoaSocioBenemeritoRequestDto` | OK |
| `CanindePwaShared\Models\Dto\TableDto\PessoaSocioBenemeritoResponseDto.cs` | `TblPessoaSocioBenemeritoResponseDto` | OK |
| `CanindePwaShared\Models\Dto\TableDto\PessoaSocioContribuinteRequestDto.cs` | `TblPessoaSocioContribuinteRequestDto` | OK |
| `CanindePwaShared\Models\Dto\TableDto\PessoaSocioContribuinteResponseDto.cs` | `TblPessoaSocioContribuinteResponseDto` | OK |
| `CanindePwaShared\Models\Dto\TableDto\PessoaSocioFundadorRequestDto.cs` | `TblPessoaSocioFundadorRequestDto` | OK |
| `CanindePwaShared\Models\Dto\TableDto\PessoaSocioFundadorResponseDto.cs` | `TblPessoaSocioFundadorResponseDto` | OK |
| `CanindePwaShared\Models\Dto\TableDto\PessoaSocioUsuarioRequestDto.cs` | `TblPessoaSocioUsuarioRequestDto` | OK |
| `CanindePwaShared\Models\Dto\TableDto\PessoaSocioUsuarioResponseDto.cs` | `TblPessoaSocioUsuarioResponseDto` | OK |
| `CanindePwaShared\Models\Dto\UserDto\ChangePasswordRequestDto.cs` | `ChangePasswordRequestDto` | OK |
| `CanindePwaShared\Models\Dto\UserDto\UpdateUserDto.cs` | `UpdateUserDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\UserDto\UserProfileDto.cs` | `UserProfileDto` | VERIFICAR (base/sem direcao) |
| `CanindePwaShared\Models\Dto\UserDto\UserProfileResponseDto.cs` | `UserProfileResponseDto` | OK |


## 5. Endpoints HTTP

| Controller | Metodo | Verbo | Rota | AllowAnonymous | Auth |
|---|---|---|---|---|---|
| AuthController | `CadastrarUsuario` | `POST` | `api/[controller]/cadastrarusuario` | ABERTO | [Authorize(Policy = "Ceo"); [Authorize(Policy = "AllowPendingPin"); [Authorize(Policy = "CeoAdmin"); [Authorize(Policy = "AllowInactiveUser") |
| AuthController | `ChangePassword` | `POST` | `api/[controller]/changepassword` | ABERTO | [Authorize(Policy = "Ceo"); [Authorize(Policy = "AllowPendingPin"); [Authorize(Policy = "CeoAdmin"); [Authorize(Policy = "AllowInactiveUser") |
| AuthController | `Confirm` | `GET` | `api/[controller]/confirm` | ABERTO | [Authorize(Policy = "Ceo"); [Authorize(Policy = "AllowPendingPin"); [Authorize(Policy = "CeoAdmin"); [Authorize(Policy = "AllowInactiveUser") |
| AuthController | `ForgotPassword` | `POST` | `api/[controller]/forgotpassword` | ABERTO | [Authorize(Policy = "Ceo"); [Authorize(Policy = "AllowPendingPin"); [Authorize(Policy = "CeoAdmin"); [Authorize(Policy = "AllowInactiveUser") |
| AuthController | `Health` | `GET` | `api/[controller]/health` | ABERTO | [Authorize(Policy = "Ceo"); [Authorize(Policy = "AllowPendingPin"); [Authorize(Policy = "CeoAdmin"); [Authorize(Policy = "AllowInactiveUser") |
| AuthController | `Login` | `POST` | `api/[controller]/login` | ABERTO | [Authorize(Policy = "Ceo"); [Authorize(Policy = "AllowPendingPin"); [Authorize(Policy = "CeoAdmin"); [Authorize(Policy = "AllowInactiveUser") |
| AuthController | `Logout` | `POST` | `api/[controller]/logout` | ABERTO | [Authorize(Policy = "Ceo"); [Authorize(Policy = "AllowPendingPin"); [Authorize(Policy = "CeoAdmin"); [Authorize(Policy = "AllowInactiveUser") |
| AuthController | `?` | `GET` | `api/[controller]/mypinstatus` | ABERTO | [Authorize(Policy = "Ceo"); [Authorize(Policy = "AllowPendingPin"); [Authorize(Policy = "CeoAdmin"); [Authorize(Policy = "AllowInactiveUser") |
| AuthController | `GetPendingPins` | `GET` | `api/[controller]/pendingpins` | ABERTO | [Authorize(Policy = "Ceo"); [Authorize(Policy = "AllowPendingPin"); [Authorize(Policy = "CeoAdmin"); [Authorize(Policy = "AllowInactiveUser") |
| AuthController | `GetPinStatus` | `GET` | `api/[controller]/pinstatus` | ABERTO | [Authorize(Policy = "Ceo"); [Authorize(Policy = "AllowPendingPin"); [Authorize(Policy = "CeoAdmin"); [Authorize(Policy = "AllowInactiveUser") |
| AuthController | `?` | `GET` | `api/[controller]/profile` | ABERTO | [Authorize(Policy = "Ceo"); [Authorize(Policy = "AllowPendingPin"); [Authorize(Policy = "CeoAdmin"); [Authorize(Policy = "AllowInactiveUser") |
| AuthController | `RefreshToken` | `POST` | `api/[controller]/refreshtoken` | ABERTO | [Authorize(Policy = "Ceo"); [Authorize(Policy = "AllowPendingPin"); [Authorize(Policy = "CeoAdmin"); [Authorize(Policy = "AllowInactiveUser") |
| AuthController | `ResendConfirmation` | `POST` | `api/[controller]/resendconfirmation` | ABERTO | [Authorize(Policy = "Ceo"); [Authorize(Policy = "AllowPendingPin"); [Authorize(Policy = "CeoAdmin"); [Authorize(Policy = "AllowInactiveUser") |
| AuthController | `ResendPin` | `POST` | `api/[controller]/resendpin` | ABERTO | [Authorize(Policy = "Ceo"); [Authorize(Policy = "AllowPendingPin"); [Authorize(Policy = "CeoAdmin"); [Authorize(Policy = "AllowInactiveUser") |
| AuthController | `ValidatePin` | `POST` | `api/[controller]/validatepin` | ABERTO | [Authorize(Policy = "Ceo"); [Authorize(Policy = "AllowPendingPin"); [Authorize(Policy = "CeoAdmin"); [Authorize(Policy = "AllowInactiveUser") |
| AuthController | `VerifyPin` | `POST` | `api/[controller]/verifypin` | ABERTO | [Authorize(Policy = "Ceo"); [Authorize(Policy = "AllowPendingPin"); [Authorize(Policy = "CeoAdmin"); [Authorize(Policy = "AllowInactiveUser") |
| GenericCrudController | `Create` | `POST` | `api/[controller]/{entity}` | FECHADO | [Authorize(Roles = "Admin") |
| GenericCrudController | `GetList` | `GET` | `api/[controller]/{entity}` | FECHADO | [Authorize(Roles = "Admin") |
| GenericCrudController | `GetById` | `GET` | `api/[controller]/{entity}/{id}` | FECHADO | [Authorize(Roles = "Admin") |
| GenericCrudController | `Update` | `PUT` | `api/[controller]/{entity}/{id}` | FECHADO | [Authorize(Roles = "Admin") |
| GenericCrudController | `Delete` | `DELETE` | `api/[controller]/{entity}/{id}` | FECHADO | [Authorize(Roles = "Admin") |
| MenuController | `Health` | `GET` | `api/menu/health` | ABERTO |  |
| MenuController | `?` | `GET` | `api/menu/porusuario` | ABERTO |  |
| PessoaController | `GetAll` | `GET` | `api/[controller]` | FECHADO |  |
| PessoaController | `CreatePessoaAsync` | `POST` | `api/[controller]` | FECHADO |  |
| PessoaController | `UpdatePessoaAsync` | `PUT` | `api/[controller]/{id:int}` | FECHADO |  |
| PessoaController | `Get` | `GET` | `api/[controller]/{id:int}` | FECHADO |  |
| PessoaEmailController | `Create` | `POST` | `api/[controller]` | FECHADO |  |
| PessoaEmailController | `Update` | `PUT` | `api/[controller]/{id:int}` | FECHADO |  |
| PessoaEmailController | `Delete` | `DELETE` | `api/[controller]/{id:int}` | FECHADO |  |
| PessoaEmailController | `GetAll` | `GET` | `api/[controller]/{idPessoa:int}` | FECHADO |  |
| PessoaEmailController | `GetById` | `GET` | `api/[controller]/email/{id:int}` | FECHADO |  |
| UsersController | `GetAll` | `GET` | `api/[controller]` | FECHADO | [Authorize(Roles = "Admin") |
| UsersController | `Create` | `POST` | `api/[controller]` | FECHADO | [Authorize(Roles = "Admin") |
| UsersController | `Update` | `PUT` | `api/[controller]/{id}` | FECHADO | [Authorize(Roles = "Admin") |
| UsersController | `Get` | `GET` | `api/[controller]/{id}` | FECHADO | [Authorize(Roles = "Admin") |
| UsersController | `Delete` | `DELETE` | `api/[controller]/{id}` | FECHADO | [Authorize(Roles = "Admin") |


## 6. DbSets

| Propriedade | Entidade |
|---|---|
| `EmailPins` | `EmailPin` |
| `Menus` | `Menu` |
| `Perfis` | `Perfil` |
| `RefreshTokens` | `RefreshToken` |
| `Roles` | `Role` |
| `Rotas` | `Rota` |
| `TblBairro` | `TblBairro` |
| `TblCep` | `TblCep` |
| `TblCidade` | `TblCidade` |
| `TblEstado` | `TblEstado` |
| `TblLogradouro` | `TblLogradouro` |
| `TblParametro` | `TblParametro` |
| `TblPessoa` | `TblPessoa` |
| `TblPessoaEmail` | `TblPessoaEmail` |
| `TblPessoaFisica` | `TblPessoaFisica` |
| `TblPessoaJuridica` | `TblPessoaJuridica` |
| `TblPessoaSocioBenemerito` | `TblPessoaSocioBenemerito` |
| `TblPessoaSocioContribuinte` | `TblPessoaSocioContribuinte` |
| `TblPessoaSocioFundador` | `TblPessoaSocioFundador` |
| `TblPessoaSocioUsuario` | `TblPessoaSocioUsuario` |
| `TempPasswords` | `TempPassword` |
| `UserRoles` | `UserRole` |
| `Users` | `User` |
| `UvwBairro` | `UvwBairro` |
| `UvwCep` | `UvwCep` |
| `UvwCidade` | `UvwCidade` |
| `UvwEstado` | `UvwEstado` |
| `UvwLogradouro` | `UvwLogradouro` |
| `UvwParametro` | `UvwParametro` |
| `UvwPessoa` | `UvwPessoa` |
| `UvwPessoaEmail` | `UvwPessoaEmail` |
| `UvwPessoaFisica` | `UvwPessoaFisica` |
| `UvwPessoaJuridica` | `UvwPessoaJuridica` |


## 7. Servicos em DI

- `GenericCrudService`
- `IAdminSeedService, AdminSeedService`
- `IAuthManager, AuthManager`
- `ICepLookupService, CepLookupService`
- `IEmailService, EmailService`
- `IMenuService, MenuService`
- `IPasswordHasher, BCryptPasswordHasher`
- `IPessoaEmailService, PessoaEmailService`
- `IPessoaService, PessoaService`
- `IPinConfigurationService, PinConfigurationService`
- `ITempPasswordService, TempPasswordService`


## 8. Enums

- `AuthModeEnum`
- `ClassificacaoSocioEnum`
- `EstatusEnum`
- `PerfilEnum`
- `ResultadoEnum`
- `SetorEnum`
- `SimNaoEnum`
- `TipoCepEnum`
- `TipoMensagemEnum`
- `UsuarioEnum`


## 9. Politicas de Autorizacao

- `AllowInactiveUser`
- `AllowPendingPin`
- `RequirePendingPin`
- `RequireActiveUser`
- `Ceo`
- `CeoAdmin`
- `CeoAdminOperador`
- `DashboardAccess`


## 10. Pipeline

1. `UseSwagger`
2. `UseSwaggerUI`
3. `UseCors` ("DevelopmentPolicy")
4. `UseMiddleware`
5. `UseHttpsRedirection`
6. `UseAuthentication`
7. `UseAuthorization`


## 11. Registry (existencia)

_Nao verificado. Use -CheckRegistryDevelopment ou -CheckRegistryProduction._


## 12. Flags de Seguranca

| Severidade | Item | Detalhe | Acao |
|---|---|---|---|
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Dto\TableDto\BairroRequestDto.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Dto\TableDto\BairroResponseDto.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Dto\TableDto\PessoaEmailRequestDto.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Dto\TableDto\PessoaEmailResponseDto.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Dto\TableDto\PessoaRequestDto.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Dto\TableDto\PessoaResponseDto.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Dto\TableDto\PessoaSocioBenemeritoRequestDto.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Dto\TableDto\PessoaSocioBenemeritoResponseDto.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Dto\TableDto\PessoaSocioContribuinteRequestDto.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Dto\TableDto\PessoaSocioContribuinteResponseDto.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Dto\TableDto\PessoaSocioFundadorRequestDto.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Dto\TableDto\PessoaSocioFundadorResponseDto.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Dto\TableDto\PessoaSocioUsuarioRequestDto.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Dto\TableDto\PessoaSocioUsuarioResponseDto.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Tbl\TblBairro.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Tbl\TblBase.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Tbl\TblCep.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Tbl\TblCidade.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Tbl\TblEstado.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Tbl\TblLogradouro.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Tbl\TblParametro.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Tbl\TblPessoa.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Tbl\TblPessoaEmail.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Tbl\TblPessoaFisica.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Tbl\TblPessoaJuridica.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Tbl\TblPessoaSocioBenemerito.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Tbl\TblPessoaSocioContribuinte.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Tbl\TblPessoaSocioFundador.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindePwaShared\Models\Tbl\TblPessoaSocioUsuario.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindeWebServer\Controllers\PessoaController.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindeWebServer\Controllers\PessoaEmailController.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindeWebServer\Data\CanindeDbContext.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindeWebServer\Services\CepLookupService.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindeWebServer\Services\IPessoaEmailService.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindeWebServer\Services\IPessoaService.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindeWebServer\Services\PessoaEmailService.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| MEDIA | Prefixo "tbl" residual | `CanindeWebServer\Services\PessoaService.cs` | Renomear entidade/tabela (migracao + atualizacao de DbSet) |
| ALTA | Endpoint publico sem rate limiting | `AuthController.Login [POST api/[controller]/login]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `AuthController.RefreshToken [POST api/[controller]/refreshtoken]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `AuthController.CadastrarUsuario [POST api/[controller]/cadastrarusuario]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `AuthController.VerifyPin [POST api/[controller]/verifypin]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `AuthController.GetPinStatus [GET api/[controller]/pinstatus]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `AuthController.? [GET api/[controller]/mypinstatus]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `AuthController.ResendPin [POST api/[controller]/resendpin]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `AuthController.GetPendingPins [GET api/[controller]/pendingpins]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `AuthController.ResendConfirmation [POST api/[controller]/resendconfirmation]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `AuthController.Confirm [GET api/[controller]/confirm]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `AuthController.ForgotPassword [POST api/[controller]/forgotpassword]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `AuthController.ValidatePin [POST api/[controller]/validatepin]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `AuthController.Logout [POST api/[controller]/logout]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `AuthController.? [GET api/[controller]/profile]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `AuthController.ChangePassword [POST api/[controller]/changepassword]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `AuthController.Health [GET api/[controller]/health]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `MenuController.? [GET api/menu/porusuario]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| ALTA | Endpoint publico sem rate limiting | `MenuController.Health [GET api/menu/health]` | Adicionar [EnableRateLimiting] + AddRateLimiter no Program.cs |
| MEDIA | AllowedHosts = "*" | `CanindeWebServer\appsettings.json` | Restringir para dominios explicitos |
| MEDIA | AllowedHosts = "*" | `CanindeWebServer\bin\Debug\net10.0\appsettings.json` | Restringir para dominios explicitos |


## 13. Secoes com Falha

_Nenhuma._

