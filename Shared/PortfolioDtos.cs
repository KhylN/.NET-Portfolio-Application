using System.Runtime.CompilerServices;

namespace SkillSnap.Shared;

public record ProjectDto(Guid Id, string Title, string Summary, string[] Tags, string? RepoUrl, string? LiveUrl);
public record ProjectCreateDto(string Title, string Summary, string[] Tags, string? RepoUrl, string? LiveUrl);
public record AuthResult(string AccessToken, DateTime ExpiresUtc, string UserName, string[] Roles);
public record RegisterRequest(string Email, string Password, string DisplayName);
public record LoginRequest(string Email, string Password);