using Microsoft.AspNetCore.Identity;

namespace SkillSnap.Server.Models;

public class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = "";
}