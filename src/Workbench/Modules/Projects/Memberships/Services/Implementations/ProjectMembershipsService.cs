using Workbench.Common.Exceptions;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Repositories;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Memberships.Dtos;
using Workbench.Modules.Projects.Memberships.Mappers;
using Workbench.Modules.Projects.Memberships.Models;
using Workbench.Modules.Projects.Memberships.Repositories;
using Workbench.Modules.Projects.Models;
using Workbench.Modules.Projects.Repositories;

namespace Workbench.Modules.Projects.Memberships.Services.Implementations;

public class ProjectMembershipsService : IProjectMembershipsService
{
    private readonly IAuthorizationGuard _authGuard;
    private readonly IIssuesRepository _issuesRepository;
    private readonly IProjectMembershipsRepository _projectMembershipsRepository;
    private readonly IProjectsRepository _projectsRepository;
    private readonly ICurrentUser _user;

    public ProjectMembershipsService(IProjectMembershipsRepository projectMembershipsRepository,
        ICurrentUser user, IProjectsRepository projectsRepository,
        IAuthorizationGuard authGuard, IIssuesRepository issuesRepository)
    {
        _projectMembershipsRepository = projectMembershipsRepository;
        _user = user;
        _projectsRepository = projectsRepository;
        _authGuard = authGuard;
        _issuesRepository = issuesRepository;
    }

    public async Task<ProjectMembershipDto?> FindCurrentUserProjectMembership(int projectId) =>
        (await _projectMembershipsRepository
            .FindMembershipByProjectIdAndUserId(projectId, _user.Id))
        ?.ToDto();

    public async Task<ProjectMembershipDto> GetProjectMembership(int projectId, string username) =>
        (await _projectMembershipsRepository.GetByProjectIdAndUsernameAsync(projectId, username))
        .ToDto();

    public Task<List<ProjectMembershipDto>> GetProjectMemberships(int projectId) =>
        _projectMembershipsRepository.GetMembershipsByProjectId(projectId);

    public async Task<bool> IsMember(int projectId, int userId) =>
        await _projectMembershipsRepository
            .FindMembershipByProjectIdAndUserId(projectId, userId) is not null;

    public async Task AddMember(int projectId, int userId, ProjectMemberRole role)
    {
        _projectMembershipsRepository.Add(new ProjectMembership
        {
            ProjectId = projectId,
            UserId = userId,
            Role = role
        });

        await _projectMembershipsRepository.SaveChangesAsync();
    }

    public async Task UpdateRole(int projectId, string username, ProjectMemberRole role)
    {
        var project = await _projectsRepository.GetByIdAsync(projectId);
        await _authGuard.AuthorizeProjectLead(project);

        var membership = await _projectMembershipsRepository
            .GetByProjectIdAndUsernameAsync(projectId, username);

        if (membership.UserId == _user.Id)
            throw new BadRequestException("Cannot change your own role");

        if (membership.UserId == project.OwnerId)
            throw new BadRequestException("Cannot change the owner's role");

        membership.Role = role;
        await _projectMembershipsRepository.SaveChangesAsync();
    }

    public async Task RemoveMember(int projectId, string username)
    {
        var project = await _projectsRepository.GetByIdAsync(projectId);
        await _authGuard.AuthorizeProjectLead(project);

        var membership = await _projectMembershipsRepository
            .GetByProjectIdAndUsernameAsync(projectId, username);

        if (membership.UserId == _user.Id)
            throw new BadRequestException("Cannot remove yourself");

        await RemoveMembership(project, membership);
    }

    public async Task LeaveProject(int projectId)
    {
        var project = await _projectsRepository.GetByIdAsync(projectId);

        var membership = await _projectMembershipsRepository
            .FindMembershipByProjectIdAndUserId(projectId, _user.Id);

        if (membership is null)
            throw new BadRequestException("You are not a member of this project");

        await RemoveMembership(project, membership);
    }

    /// <summary>
    ///     Removes a membership from a project and unassigns the user from all issues in that project.
    /// </summary>
    /// <param name="project">The project from which the membership is being removed.</param>
    /// <param name="membership">The membership to be removed.</param>
    /// <exception cref="BadRequestException">Thrown if the membership belongs to the project owner.</exception>
    private async Task RemoveMembership(Project project, ProjectMembership membership)
    {
        if (membership.UserId == project.OwnerId)
            throw new BadRequestException("Cannot remove the project owner");

        await _issuesRepository.UnassignFromAllAsync(project.Id, membership.UserId);
        _projectMembershipsRepository.Remove(membership);

        await _projectMembershipsRepository.SaveChangesAsync();
    }
}