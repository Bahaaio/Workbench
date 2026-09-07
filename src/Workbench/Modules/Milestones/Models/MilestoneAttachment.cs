using Workbench.Modules.Attachments.Models;

namespace Workbench.Modules.Milestones.Models;

public class MilestoneAttachment : Attachment, IHasParent<Milestone>
{
    public int ParentId { get; set; }
    public Milestone Milestone { get; set; } = null!;
}
