using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

/// <summary>
/// Navegação segura do cliente.
/// "Menu" = /dashboard: não existe página /menu (antes o botão "Voltar ao Menu" caía em 404).
/// </summary>
public class NavigationService
{
    private readonly NavigationManager _navigation;
    private readonly AuthenticationStateProvider _authProvider;

    public NavigationService(
        NavigationManager navigation,
        AuthenticationStateProvider authProvider)
    {
        _navigation = navigation;
        _authProvider = authProvider;
    }

    public async Task GoBackToStartAsync()
    {
        var state = await _authProvider.GetAuthenticationStateAsync();

        if (state.User.Identity?.IsAuthenticated == true)
            _navigation.NavigateTo("/dashboard");
        else
            _navigation.NavigateTo("/login");
    }

    public void GoToMenu()
        => _navigation.NavigateTo("/dashboard");

    public void GoToLogin()
        => _navigation.NavigateTo("/login", forceLoad: true);

}
