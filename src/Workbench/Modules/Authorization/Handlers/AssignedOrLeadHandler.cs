using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Workbench.Data;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Requirements;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Memberships.Models;

namespace Workbench.Modules.Authorization.Handlers;

public class AssignedOrLeadHandler : AuthorizationHandler<AssignedOrLeadRequirement, Issue>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public AssignedOrLeadHandler(ICurrentUser user, AppDbContext db)
    {
        _user = user;
        _db = db;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AssignedOrLeadRequirement requirement,
        Issue resource)
    {
        if (resource.AssignedToId == _user.Id)
        {
            context.Succeed(requirement);
            return;
        }

        var membership = await _db.ProjectMemberships
            .SingleOrDefaultAsync(pm =>
                pm.ProjectId == resource.ProjectId && pm.UserId == _user.Id);

        if (membership?.Role == ProjectMemberRole.Lead)
            context.Succeed(requirement);
    }
}
