using Workbench.Common.Models;
using Workbench.Modules.Auth.Models;
using Workbench.Modules.Authorization.Models;
using Workbench.Modules.Issues.Models;

namespace Workbench.Modules.Comments.Models;

public class Comment : IEntity<int>, IOwnedByUser, IBelongsToProject
{
    public int Id { get; set; }
    public int OwnerId => AuthorId;
    public int ProjectId => Issue.ProjectId;

    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public required int AuthorId { get; set; }
    public ApplicationUser Author { get; set; } = null!;

    public required int IssueId { get; set; }
    public Issue Issue { get; set; } = null!;

    public ICollection<CommentAttachment> Attachments { get; set; } = [];
}
