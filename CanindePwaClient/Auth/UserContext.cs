using CanindePwaShared.Models.Dto.UserDto;
using CanindePwaShared.Models.Dto.AuthDto;
using CanindePwaClient.Services;

namespace CanindePwaClient.Auth
{
    public class UserContext
    {
        private readonly IApiService _apiService;

        // Carga em andamento (evita duas cargas simultâneas: GuardaRota + DashboardLayout)
        private Task? _loadTask;

        public UserProfileDto? CurrentUser { get; private set; }
        public IReadOnlyList<MenuItemDto>? Menus { get; private set; }

        public bool IsLoaded { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;

        public event Action? OnChange;

        public UserContext(IApiService apiService)
        {
            _apiService = apiService;
        }

        // ============================
        // CARGA CENTRALIZADA
        // ============================
        public Task LoadAsync()
        {
            // Se já existe uma carga em andamento, quem chamou espera por ela.
            // Depois de concluída, uma nova chamada recarrega normalmente.
            if (_loadTask is { IsCompleted: false })
                return _loadTask;

            _loadTask = CarregarAsync();
            return _loadTask;
        }

        private async Task CarregarAsync()
        {
            try
            {
                var profileResponse =
                    await _apiService.GetAsync<UserProfileResponseDto>("api/auth/profile");

                CurrentUser = profileResponse?.User;

                if (CurrentUser != null)
                {
                    Menus = await _apiService
                        .GetAsync<IReadOnlyList<MenuItemDto>>("api/menu/porusuario");
                }
                else
                {
                    Menus = null;
                }
            }
            catch
            {
                CurrentUser = null;
                Menus = null;
            }
            finally
            {
                IsLoaded = true;
                OnChange?.Invoke();
            }
        }

        // ============================
        // RESET (LOGOUT / EXPIRAÇÃO)
        // ============================
        public void Clear()
        {
            CurrentUser = null;
            Menus = null;
            IsLoaded = false;
            OnChange?.Invoke();
        }

        // ============================
        // CONTROLE DE ACESSO POR ROTA
        // ============================
        // FONTE ÚNICA: a mesma lista de menus (tabelas perfis/rotas/menus, via
        // api/menu/porusuario) que monta a sidebar decide quais páginas o usuário abre.
        // Rota fora da lista = sem acesso, mesmo digitando a URL.
        // FALHA FECHADA: sem menus carregados, nega.
        // Subpáginas valem pela rota pai (/cadastrarpessoa libera /cadastrarpessoa/12),
        // igual ao NavLinkMatch.Prefix da sidebar.
        // Isto controla NAVEGAÇÃO; a segurança real dos dados continua no servidor.
        public bool PodeAcessarRota(string caminho)
        {
            if (Menus == null)
                return false;

            var destino = NormalizarCaminho(caminho);

            foreach (var menu in Menus)
            {
                if (!menu.Ativo)
                    continue;

                var permitido = NormalizarCaminho(menu.Caminho);

                if (permitido == "/")
                    continue; // "/" nunca libera as demais rotas por prefixo

                if (destino == permitido ||
                    destino.StartsWith(permitido + "/", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        // Padroniza para comparar: sem query/fragmento, barra inicial única,
        // sem barra final, minúsculo.  "Cadastrar/?x=1" -> "/cadastrar"
        public static string NormalizarCaminho(string? caminho)
        {
            var c = (caminho ?? string.Empty).Trim();

            var corte = c.IndexOfAny(new[] { '?', '#' });
            if (corte >= 0)
                c = c[..corte];

            return ("/" + c.Trim('/')).ToLowerInvariant();
        }
    }
}
