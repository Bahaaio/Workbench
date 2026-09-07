using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Data;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Memberships.Dtos;
using Workbench.Modules.Projects.Memberships.Mappers;
using Workbench.Modules.Projects.Memberships.Models;
using Workbench.Modules.Projects.Models;

namespace Workbench.Modules.Projects.Memberships.Services.Implementations;

public class ProjectMembershipsService : IProjectMembershipsService
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationGuard _authGuard;
    private readonly ICurrentUser _user;

    public ProjectMembershipsService(AppDbContext dbContext,
        ICurrentUser user,
        IAuthorizationGuard authGuard)
    {
        _db = dbContext;
        _user = user;
        _authGuard = authGuard;
    }

    public async Task<ProjectMembershipDto?> FindCurrentUserProjectMembership(int projectId) =>
        (await _db.ProjectMemberships
            .Include(pm => pm.User)
            .SingleOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == _user.Id))
        ?.ToDto();

    public async Task<ProjectMembershipDto> GetProjectMembership(int projectId, string username)
    {
        var membership = await _db.ProjectMemberships
            .Include(pm => pm.User)
            .SingleOrDefaultAsync(pm => pm.ProjectId == projectId && pm.User.UserName == username)
            ?? throw new NotFoundException($"Member '{username}' not found in project");

        return membership.ToDto();
    }

    public Task<List<ProjectMembershipDto>> GetProjectMemberships(int projectId) =>
        _db.ProjectMemberships
            .Where(m => m.ProjectId == projectId)
            .Select(ProjectMembershipMapper.ToDtoExpression)
            .ToListAsync();

    public async Task<bool> IsMember(int projectId, int userId) =>
        await _db.ProjectMemberships
            .Include(pm => pm.User)
            .SingleOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId) is not null;

    public async Task AddMember(int projectId, int userId, ProjectMemberRole role)
    {
        _db.ProjectMemberships.Add(new ProjectMembership
        {
            ProjectId = projectId,
            UserId = userId,
            Role = role
        });

        await _db.SaveChangesAsync();
    }

    public async Task UpdateRole(int projectId, string username, ProjectMemberRole role)
    {
        var project = await _db.Projects
            .Include(p => p.Owner)
            .SingleOrDefaultAsync(p => p.Id == projectId)
            ?? throw new NotFoundException($"Project with id {projectId} not found");

        await _authGuard.AuthorizeProjectLead(project);

        var membership = await _db.ProjectMemberships
            .Include(pm => pm.User)
            .SingleOrDefaultAsync(pm => pm.ProjectId == projectId && pm.User.UserName == username)
            ?? throw new NotFoundException($"Member '{username}' not found in project");

        if (membership.UserId == _user.Id)
            throw new BadRequestException("Cannot change your own role");

        if (membership.UserId == project.OwnerId)
            throw new BadRequestException("Cannot change the owner's role");

        membership.Role = role;
        await _db.SaveChangesAsync();
    }

    public async Task RemoveMember(int projectId, string username)
    {
        var project = await _db.Projects
            .Include(p => p.Owner)
            .SingleOrDefaultAsync(p => p.Id == projectId)
            ?? throw new NotFoundException($"Project with id {projectId} not found");

        await _authGuard.AuthorizeProjectLead(project);

        var membership = await _db.ProjectMemberships
            .Include(pm => pm.User)
            .SingleOrDefaultAsync(pm => pm.ProjectId == projectId && pm.User.UserName == username)
            ?? throw new NotFoundException($"Member '{username}' not found in project");

        if (membership.UserId == _user.Id)
            throw new BadRequestException("Cannot remove yourself");

        await RemoveMembership(project, membership);
    }

    public async Task LeaveProject(int projectId)
    {
        var project = await _db.Projects
            .Include(p => p.Owner)
            .SingleOrDefaultAsync(p => p.Id == projectId)
            ?? throw new NotFoundException($"Project with id {projectId} not found");

        var membership = await _db.ProjectMemberships
            .Include(pm => pm.User)
            .SingleOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == _user.Id);

        if (membership is null)
            throw new BadRequestException("You are not a member of this project");

        await RemoveMembership(project, membership);
    }

    private async Task RemoveMembership(Project project, ProjectMembership membership)
    {
        if (membership.UserId == project.OwnerId)
            throw new BadRequestException("Cannot remove the project owner");

        await _db.Issues
            .Where(i =>
                i.ProjectId == project.Id &&
                i.AssignedToId == membership.UserId &&
                i.Status != Status.Closed)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(i => i.AssignedToId, default(int?)));

        _db.ProjectMemberships.Remove(membership);

        await _db.SaveChangesAsync();
    }
}
