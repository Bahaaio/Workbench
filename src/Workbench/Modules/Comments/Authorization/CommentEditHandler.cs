using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Workbench.Data;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Models;
using Workbench.Modules.Comments.Models;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Memberships.Models;

namespace Workbench.Modules.Comments.Authorization;

public class CommentEditHandler : AuthorizationHandler<CommentEditRequirement, Comment>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public CommentEditHandler(AppDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CommentEditRequirement requirement,
        Comment comment)
    {
        var projectId = comment.Issue.ProjectId;

        // Leads can always edit
        var isLead = await _db.ProjectMemberships
            .AnyAsync(m => m.ProjectId == projectId
                && m.UserId == _user.Id
                && m.Role == ProjectMemberRole.Lead);

        if (isLead)
        {
            context.Succeed(requirement);
            return;
        }

        // Within5 minutes: owner or any project member can edit
        if (DateTime.UtcNow - comment.CreatedAt <= TimeSpan.FromMinutes(5))
        {
            if (comment.AuthorId == _user.Id)
            {
                context.Succeed(requirement);
                return;
            }

            var isMember = await _db.ProjectMemberships
                .AnyAsync(m => m.ProjectId == projectId && m.UserId == _user.Id);

            if (isMember)
                context.Succeed(requirement);
        }
    }
}
