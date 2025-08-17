using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using SkillSnap.Server.Data;
using SkillSnap.Server.Models;
using SkillSnap.Shared;
using System.Threading;

namespace SkillSnap.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<ProjectsController> _log;
    private readonly IMemoryCache _cache;

    public ProjectsController(AppDbContext db, IMapper mapper, ILogger<ProjectsController> log, IMemoryCache cache)
        => (_db, _mapper, _log, _cache) = (db, mapper, log, cache);

    // GET: /api/projects?q=...&skip=0&take=20
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> Get([FromQuery] string? q, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var key = $"projects:{q}:{skip}:{take}";
        if (_cache.TryGetValue(key, out IEnumerable<ProjectDto> cached))
            return Ok(cached);

        var query = _db.Projects.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Title.Contains(q) || p.Summary.Contains(q));

        var items = await query
            .OrderByDescending(p => p.CreatedUtc)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 50))
            .ProjectTo<ProjectDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        _cache.Set(
            key,
            items,
            new MemoryCacheEntryOptions()
                .AddExpirationToken(CacheTags.ProjectsChangeToken)
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(30)) // short TTL cache
        );

        return Ok(items);
    }

    // GET: /api/projects/{id}
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id)
    {
        var key = $"project:{id}";
        if (_cache.TryGetValue(key, out ProjectDto cached))
            return Ok(cached);

        var dto = await _db.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .ProjectTo<ProjectDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        if (dto is null) return NotFound();

        _cache.Set(
            key,
            dto,
            new MemoryCacheEntryOptions()
                .AddExpirationToken(CacheTags.ProjectsChangeToken)
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(30))
        );

        return Ok(dto);
    }

    // POST: /api/projects
    [HttpPost]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ProjectDto>> Create(ProjectCreateDto dto)
    {
        var entity = _mapper.Map<Project>(dto);
        _db.Add(entity);
        await _db.SaveChangesAsync();

        var result = _mapper.Map<ProjectDto>(entity);

        // Invalidate all project-related cache entries
        CacheTags.TriggerProjectsBust();

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // DELETE: /api/projects/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Projects.FindAsync(id);
        if (entity is null) return NotFound();

        _db.Remove(entity);
        await _db.SaveChangesAsync();

        // Invalidate all project-related cache entries
        CacheTags.TriggerProjectsBust();

        return NoContent();
    }
}

// ---- Cache tag helper ----
static class CacheTags
{
    // token used to invalidate all "projects"-related entries
    private static CancellationTokenSource _projectsToken = new();

    public static IChangeToken ProjectsChangeToken => new CancellationChangeToken(_projectsToken.Token);

    public static void TriggerProjectsBust()
    {
        var old = Interlocked.Exchange(ref _projectsToken, new CancellationTokenSource());
        try { old.Cancel(); }
        finally { old.Dispose(); }
    }
}
