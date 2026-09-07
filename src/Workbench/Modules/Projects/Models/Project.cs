using Workbench.Common.Models;
using Workbench.Modules.Auth.Models;
using Workbench.Modules.Authorization.Models;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Kanban.Models;
using Workbench.Modules.Milestones.Models;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Memberships.Models;
using Workbench.Modules.Tags.Models;

namespace Workbench.Modules.Projects.Models;

public class Project : IEntity<int>, IOwnedByUser, IBelongsToProject
{
    public int Id { get; set; }

    public required int OwnerId { get; set; }
    public int ProjectId => Id;
    public ApplicationUser Owner { get; set; } = null!;

    public required string Name { get; set; } = string.Empty;
    public required string? Description { get; set; }

    public required ProjectVisibility Visibility { get; set; }
    public DateTime CreatedAt { get; set; }

    public Board Board { get; set; } = null!;

    public ICollection<Issue> Issues { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<ProjectMembership> Members { get; set; } = [];
    public ICollection<Milestone> Milestones { get; set; } = [];
}