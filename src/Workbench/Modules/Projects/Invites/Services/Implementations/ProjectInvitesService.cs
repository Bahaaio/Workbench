using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Invites.Dtos;
using Workbench.Modules.Projects.Invites.Dtos.Requests;
using Workbench.Modules.Projects.Invites.Models;
using Workbench.Modules.Projects.Memberships.Services;
using Workbench.Modules.Projects.Models;

namespace Workbench.Modules.Projects.Invites.Services.Implementations;

public class ProjectInvitesService : IProjectInvitesService
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationGuard _authGuard;
    private readonly IProjectMembershipsService _membershipsService;
    private readonly ITokensService _tokensService;
    private readonly ICurrentUser _user;

    public ProjectInvitesService(AppDbContext dbContext,
        ITokensService tokensService,
        ICurrentUser user,
        IAuthorizationGuard authGuard, IProjectMembershipsService membershipsService)
    {
        _db = dbContext;
        _tokensService = tokensService;
        _user = user;
        _authGuard = authGuard;
        _membershipsService = membershipsService;
    }

    public async Task<InviteDto> Create(CreateInviteRequest request)
    {
        var project = await _db.Projects
            .Include(p => p.Owner)
            .SingleOrDefaultAsync(p => p.Id == request.ProjectId)
            ?? throw new NotFoundException($"Project with id {request.ProjectId} not found");

        await _authGuard.AuthorizeProjectLead(project);

        var invite = new ProjectInvite
        {
            Code = _tokensService.Generate(8),
            ProjectId = request.ProjectId,
            CreatedById = _user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(request.ValidDays)
        };

        _db.ProjectInvites.Add(invite);
        await _db.SaveChangesAsync();

        return new InviteDto(invite.Code, invite.ExpiresAt);
    }

    public async Task<List<InviteDto>> GetActive(int projectId)
    {
        var project = await _db.Projects
            .Include(p => p.Owner)
            .SingleOrDefaultAsync(p => p.Id == projectId)
            ?? throw new NotFoundException($"Project with id {projectId} not found");

        await _authGuard.AuthorizeProjectLead(project);

        return await _db.ProjectInvites
            .Where(i => i.ProjectId == projectId && i.ExpiresAt > DateTime.UtcNow)
            .Select(i => new InviteDto(i.Code, i.ExpiresAt))
            .ToListAsync();
    }

    public async Task Consume(string code)
    {
        var invite = await _db.ProjectInvites.FindAsync(code);

        if (invite is null || invite.ExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("Invalid or expired invite code");

        if (await _membershipsService.IsMember(invite.ProjectId, _user.Id))
            throw new ConflictException("You are already a member of this project");

        _db.ProjectInvites.Remove(invite);
        await _db.SaveChangesAsync();

        await _membershipsService.AddMember(invite.ProjectId, _user.Id, ProjectMemberRole.Member);
    }

    public async Task Revoke(string code)
    {
        var invite = await _db.ProjectInvites.FindOrThrowAsync(code);
        await _authGuard.AuthorizeProjectLead(invite);

        _db.ProjectInvites.Remove(invite);
        await _db.SaveChangesAsync();
    }
}
