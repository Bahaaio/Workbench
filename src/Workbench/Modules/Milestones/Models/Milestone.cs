using Workbench.Common.Models;
using Workbench.Modules.Authorization.Models;
using Workbench.Modules.Projects.Models;

namespace Workbench.Modules.Milestones.Models;

public class Milestone : IEntity<int>, IBelongsToProject
{
    public int Id { get; set; }
    public required string Name { get; set; } = string.Empty;
    public required string? Description { get; set; }
    public required DateTime? DueDate { get; set; }

    public required int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public ICollection<MilestoneItem> MilestoneItems { get; set; } = [];
    public ICollection<MilestoneAttachment> Attachments { get; set; } = [];
}