namespace SkillSnap.Server.Models;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string TagsCsv { get; set; } = "";
    public string? RepoUrl { get; set; }
    public string? LiveUrl { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
}