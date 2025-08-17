using System.Runtime.CompilerServices;
using FluentValidation;
using SkillSnap.Shared;

namespace SkillSnap.Server.Validaiton;

public class ProjectCreateValidator : AbstractValidator<ProjectCreateDto>
{
    public ProjectCreateValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(2000);
        RuleForEach(x => x.Tags).NotEmpty().MaximumLength(40);
        RuleFor(x => x.RepoUrl).MaximumLength(300).When(x =>!string.IsNullOrWhiteSpace(x.RepoUrl));
        RuleFor(x => x.LiveUrl).MaximumLength(300).When(x => !string.IsNullOrWhiteSpace(x.LiveUrl));
    }
}