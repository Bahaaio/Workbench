using Microsoft.EntityFrameworkCore;
using Workbench.Data;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Kanban.Services;
using Workbench.Modules.Projects.Dtos;
using Workbench.Modules.Projects.Dtos.Requests;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Mappers;
using Workbench.Modules.Projects.Memberships.Services;
using Workbench.Modules.Projects.Models;

namespace Workbench.Modules.Projects.Services.Implementations;

public class ProjectsService : IProjectsService
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationGuard _authGuard;
    private readonly IBoardsService _boardsService;
    private readonly IProjectMembershipsService _projectMembershipsService;
    private readonly ICurrentUser _user;

    public ProjectsService(AppDbContext dbContext,
        IProjectMembershipsService projectMembershipsService,
        IBoardsService boardsService,
        ICurrentUser user, IAuthorizationGuard authGuard)
    {
        _db = dbContext;
        _projectMembershipsService = projectMembershipsService;
        _boardsService = boardsService;
        _user = user;
        _authGuard = authGuard;
    }

    public Task<List<ProjectDto>> GetAll() =>
        _db.Projects.Select(ProjectMapper.ToDtoExpression).ToListAsync();

    public Task<List<ProjectDto>> GetCurrentUserProjects() =>
        _db.Projects
            .Where(p => p.OwnerId == _user.Id || p.Members.Any(m => m.UserId == _user.Id))
            .Select(ProjectMapper.ToDtoExpression)
            .ToListAsync();

    public async Task<ProjectDto> GetById(int id)
    {
        var project = await _db.Projects
            .Include(p => p.Owner)
            .SingleOrDefaultAsync(p => p.Id == id)
            ?? throw new Common.Exceptions.NotFoundException($"Project with id {id} not found");

        return project.ToDto();
    }

    public async Task<ProjectDto> Create(CreateProjectRequest request)
    {
        var project = new Project
        {
            OwnerId = _user.Id,
            Name = request.Name,
            Description = request.Description
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        await _projectMembershipsService.AddMember(project.Id, _user.Id, ProjectMemberRole.Lead);
        await _boardsService.CreateEmpty(project.Id);

        await _db.Entry(project).Reference(p => p.Owner).LoadAsync();

        return project.ToDto();
    }

    public async Task<ProjectDto> Update(int id, UpdateProjectRequest request)
    {
        var project = await _db.Projects
            .Include(p => p.Owner)
            .SingleOrDefaultAsync(p => p.Id == id)
            ?? throw new Common.Exceptions.NotFoundException($"Project with id {id} not found");

        await _authGuard.AuthorizeOwner(project);

        project.Name = request.Name;
        if (request.Description is not null) project.Description = request.Description;

        await _db.SaveChangesAsync();

        return project.ToDto();
    }

    public async Task Delete(int id)
    {
        var project = await _db.Projects
            .Include(p => p.Owner)
            .SingleOrDefaultAsync(p => p.Id == id)
            ?? throw new Common.Exceptions.NotFoundException($"Project with id {id} not found");

        await _authGuard.AuthorizeOwner(project);

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
    }
}