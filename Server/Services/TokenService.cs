using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SkillSnap.Server.Models;

namespace SkillSnap.Server.Services;

public class TokenService
{
    private readonly IConfiguration _cfg;
    private readonly UserManager<AppUser> _um;

    public TokenService(IConfiguration cfg, UserManager<AppUser> um) { _cfg = cfg; _um = um; }

    public async Task<(string token, DateTime expires, string[] roles)> CreateAsync(AppUser user)
    {
        var roles = (await _um.GetRolesAsync(user)).ToArray();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new("name", user.DisplayName ?? user.Email ?? ""),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Auth:Key"] ?? "sEcretKey123!)"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(2);

        var jwt = new JwtSecurityToken(
            issuer: _cfg["Auth:Issuer"], audience: _cfg["Auth:Audience"],
            claims: claims, notBefore: DateTime.UtcNow, expires: expires, signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires, roles);
    }
}