using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Workbench.Data;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Models;
using Workbench.Modules.Authorization.Requirements;
using Workbench.Modules.Projects.Memberships.Models;

namespace Workbench.Modules.Authorization.Handlers;

public class OwnerOrTeamMemberHandler :
    AuthorizationHandler<OwnerOrTeamMemberRequirement, IBelongsToProject>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public OwnerOrTeamMemberHandler(ICurrentUser user, AppDbContext db)
    {
        _user = user;
        _db = db;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnerOrTeamMemberRequirement requirement,
        IBelongsToProject resource)
    {
        if (resource is IOwnedByUser ownedResource && ownedResource.OwnerId == _user.Id)
        {
            context.Succeed(requirement);
            return;
        }

        var membership = await _db.ProjectMemberships
            .SingleOrDefaultAsync(pm =>
                pm.ProjectId == resource.ProjectId && pm.UserId == _user.Id);

        if (membership is not null)
            context.Succeed(requirement);
    }
}
