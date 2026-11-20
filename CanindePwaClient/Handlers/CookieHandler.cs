using CanindePwaClient.Auth;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace CanindePwaClient.Handlers
{
    /// <summary>
    /// Handler exclusivo para forcar envio de cookies HTTPOnly
    /// Responsabilidade unica: credentials: include
    /// </summary>
    public class CookieHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}

