using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

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
            _navigation.NavigateTo("/menu");
        else
            _navigation.NavigateTo("/login");
    }

    public void GoToMenu()
        => _navigation.NavigateTo("/menu");

    public void GoToLogin()
        => _navigation.NavigateTo("/login", forceLoad: true);

}
