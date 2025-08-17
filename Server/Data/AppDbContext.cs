using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Server.Models;

namespace SkillSnap.Server.Data;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public DbSet<Project> Projects => Set<Project>();
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Project>(e =>
        {
            e.Property(p => p.Title).HasMaxLength(120).IsRequired();
            e.Property(p => p.Summary).HasMaxLength(2000).IsRequired();
            e.Property(p => p.TagsCsv).HasMaxLength(400);
            e.HasIndex(p => p.CreatedUtc);
            e.HasIndex(p => p.Title);
        });
    }
}