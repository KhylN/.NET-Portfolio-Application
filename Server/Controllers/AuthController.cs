using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SkillSnap.Server.Models;
using SkillSnap.Server.Services;
using SkillSnap.Shared;

namespace SkillSnap.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _um;
    private readonly SignInManager<AppUser> _sm;
    private readonly RoleManager<IdentityRole<Guid>> _rm;
    private readonly TokenService _tokens;

    public AuthController(UserManager<AppUser> um, SignInManager<AppUser> sm, RoleManager<IdentityRole<Guid>> rm, TokenService tokens)
        => (_um, _sm, _rm, _tokens) = (um, sm, rm, tokens);

    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register(RegisterRequest req)
    {
        var user = new AppUser { UserName = req.Email, Email = req.Email, DisplayName = req.DisplayName };
        var result = await _um.CreateAsync(user, req.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);

        // ensure default roles exist
        if (!await _rm.RoleExistsAsync("Owner")) await _rm.CreateAsync(new IdentityRole<Guid>("Owner"));
        if (!await _rm.RoleExistsAsync("Viewer")) await _rm.CreateAsync(new IdentityRole<Guid>("Viewer"));
        await _um.AddToRoleAsync(user, "Owner");

        var (token, exp, roles) = await _tokens.CreateAsync(user);
        return new AuthResult(token, exp, user.DisplayName, roles);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login(LoginRequest req)
    {
        var user = await _um.FindByEmailAsync(req.Email);
        if (user is null) return Unauthorized();
        var ok = await _sm.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
        if (!ok.Succeeded) return Unauthorized();

        var (token, exp, roles) = await _tokens.CreateAsync(user);
        return new AuthResult(token, exp, user.DisplayName, roles);
    }
}