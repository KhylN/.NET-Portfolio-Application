using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace SkillSnap.Client.Services.Auth;

public class TokenAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _store;
    public TokenAuthStateProvider(ILocalStorageService store) => _store = store;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _store.GetItemAsStringAsync("jwt");
        ClaimsIdentity identity;

        if (string.IsNullOrWhiteSpace(token))
        {
            identity = new ClaimsIdentity(); // anonymous
        }
        else
        {
            // Minimal identity; you can parse real claims if you like.
            identity = new ClaimsIdentity(new[] { new Claim("jwt", token) }, authenticationType: "jwt");
        }

        var user = new ClaimsPrincipal(identity);
        return new AuthenticationState(user);
    }

    public Task NotifyUserChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return Task.CompletedTask;
    }
}
