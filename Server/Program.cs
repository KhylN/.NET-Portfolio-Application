using System.Text;
using AutoMapper;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkillSnap.Server.Data;
using SkillSnap.Server.Mapping;
using SkillSnap.Server.Models;
using SkillSnap.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// EF + Sqlite (file db)
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("db") ??
                "Data Source=skillsnap.db"));

// Identity + roles
builder.Services.AddIdentityCore<AppUser>(o =>
{
    o.Password.RequiredLength = 6;
    o.User.RequireUniqueEmail = true;
})
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

// JWT
var key = builder.Configuration["Auth:Key"] ?? "dev_secret_key_please_change";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new()
        {
            ValidateIssuer = false, ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddMemoryCache();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
builder.Services.AddScoped<TokenService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// seed DB (one admin if empty)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var um = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    if (!await rm.RoleExistsAsync("Owner")) await rm.CreateAsync(new IdentityRole<Guid>("Owner"));
    if (!await rm.RoleExistsAsync("Viewer")) await rm.CreateAsync(new IdentityRole<Guid>("Viewer"));

    if ((await um.Users.CountAsync()) == 0)
    {
        var admin = new AppUser { UserName = "admin@example.com", Email = "admin@example.com", DisplayName = "Admin" };
        await um.CreateAsync(admin, "Admin!23");
        await um.AddToRoleAsync(admin, "Owner");
    }
}

app.Run();
