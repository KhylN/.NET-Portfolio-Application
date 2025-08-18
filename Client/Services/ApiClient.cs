using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using SkillSnap.Shared;

namespace SkillSnap.Client.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _store;

    public ApiClient(HttpClient http, ILocalStorageService store)
    {
        _http = http;
        _store = store;
    }

    private async Task AttachTokenAsync()
    {
        var token = await _store.GetItemAsStringAsync("jwt");
        _http.DefaultRequestHeaders.Authorization =
            string.IsNullOrWhiteSpace(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<bool> LoginAsync(LoginRequest req)
    {
        var resp = await _http.PostAsJsonAsync("api/auth/login", req);
        if (!resp.IsSuccessStatusCode) return false;

        var auth = await resp.Content.ReadFromJsonAsync<AuthResult>();
        if (auth is null) return false;

        await _store.SetItemAsync("jwt", auth.AccessToken);
        return true;
    }

    public async Task<bool> RegisterAsync(RegisterRequest req)
    {
        var resp = await _http.PostAsJsonAsync("api/auth/register", req);
        if (!resp.IsSuccessStatusCode) return false;

        var auth = await resp.Content.ReadFromJsonAsync<AuthResult>();
        if (auth is null) return false;

        await _store.SetItemAsync("jwt", auth.AccessToken);
        return true;
    }

    public async Task<ProjectDto[]> GetProjectsAsync(string? q = null, int skip = 0, int take = 20)
    {
        var url = $"api/projects?q={Uri.EscapeDataString(q ?? "")}&skip={skip}&take={take}";
        return await _http.GetFromJsonAsync<ProjectDto[]>(url) ?? Array.Empty<ProjectDto>();
    }

    public async Task<bool> CreateProjectAsync(ProjectCreateDto dto)
    {
        await AttachTokenAsync();
        var resp = await _http.PostAsJsonAsync("api/projects", dto);
        return resp.IsSuccessStatusCode;
    }

    public async Task LogoutAsync()
    {
        await _store.RemoveItemAsync("jwt");
        // Auth state provider will be notified by the caller
    }
}
