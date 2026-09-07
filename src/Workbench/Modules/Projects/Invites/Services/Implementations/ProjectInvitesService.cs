using Workbench.Common.Exceptions;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Invites.Dtos;
using Workbench.Modules.Projects.Invites.Dtos.Requests;
using Workbench.Modules.Projects.Invites.Models;
using Workbench.Modules.Projects.Invites.Repositories;
using Workbench.Modules.Projects.Memberships.Services;
using Workbench.Modules.Projects.Repositories;

namespace Workbench.Modules.Projects.Invites.Services.Implementations;

public class ProjectInvitesService : IProjectInvitesService
{
    private readonly IAuthorizationGuard _authGuard;
    private readonly IProjectMembershipsService _membershipsService;
    private readonly IProjectInvitesRepository _projectInvitesRepository;
    private readonly IProjectsRepository _projectsRepository;
    private readonly ITokensService _tokensService;
    private readonly ICurrentUser _user;

    public ProjectInvitesService(IProjectInvitesRepository projectInvitesRepository,
        ITokensService tokensService,
        ICurrentUser user, IProjectsRepository projectsRepository,
        IAuthorizationGuard authGuard, IProjectMembershipsService membershipsService)
    {
        _projectInvitesRepository = projectInvitesRepository;
        _tokensService = tokensService;
        _user = user;
        _projectsRepository = projectsRepository;
        _authGuard = authGuard;
        _membershipsService = membershipsService;
    }

    public async Task<InviteDto> Create(CreateInviteRequest request)
    {
        var project = await _projectsRepository.GetByIdAsync(request.ProjectId);
        await _authGuard.AuthorizeProjectLead(project);

        var invite = new ProjectInvite
        {
            Code = _tokensService.Generate(8),
            ProjectId = request.ProjectId,
            CreatedById = _user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(request.ValidDays)
        };

        _projectInvitesRepository.Add(invite);
        await _projectInvitesRepository.SaveChangesAsync();

        return new InviteDto(invite.Code, invite.ExpiresAt);
    }

    public async Task<List<InviteDto>> GetActive(int projectId)
    {
        var project = await _projectsRepository.GetByIdAsync(projectId);
        await _authGuard.AuthorizeProjectLead(project);

        return await _projectInvitesRepository.GetActiveByProjectId(projectId);
    }

    public async Task Consume(string code)
    {
        var invite = await _projectInvitesRepository.FindAsync(code);

        if (invite is null || invite.ExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("Invalid or expired invite code");

        if (await _membershipsService.IsMember(invite.ProjectId, _user.Id))
            throw new ConflictException("You are already a member of this project");

        _projectInvitesRepository.Remove(invite);
        await _projectInvitesRepository.SaveChangesAsync();

        await _membershipsService.AddMember(invite.ProjectId, _user.Id, ProjectMemberRole.Member);
    }

    public async Task Revoke(string code)
    {
        var invite = await _projectInvitesRepository.GetByIdAsync(code);
        await _authGuard.AuthorizeProjectLead(invite);

        _projectInvitesRepository.Remove(invite);
        await _projectInvitesRepository.SaveChangesAsync();
    }
}