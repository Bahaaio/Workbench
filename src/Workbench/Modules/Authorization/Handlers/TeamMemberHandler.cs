using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Workbench.Data;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Models;
using Workbench.Modules.Authorization.Requirements;
using Workbench.Modules.Projects.Memberships.Models;

namespace Workbench.Modules.Authorization.Handlers;

public class TeamMemberHandler : AuthorizationHandler<TeamMemberRequirement, IBelongsToProject>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public TeamMemberHandler(AppDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TeamMemberRequirement requirement,
        IBelongsToProject resource)
    {
        var membership = await _db.ProjectMemberships
            .SingleOrDefaultAsync(pm =>
                pm.ProjectId == resource.ProjectId && pm.UserId == _user.Id);

        if (membership is not null)
            context.Succeed(requirement);
    }
}
