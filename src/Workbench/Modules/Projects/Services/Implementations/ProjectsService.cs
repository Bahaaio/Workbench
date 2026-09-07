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
using Workbench.Modules.Projects.Repositories;

namespace Workbench.Modules.Projects.Services.Implementations;

public class ProjectsService : IProjectsService
{
    private readonly IAuthorizationGuard _authGuard;
    private readonly IBoardsService _boardsService;
    private readonly IProjectMembershipsService _projectMembershipsService;
    private readonly IProjectsRepository _projectsRepository;
    private readonly ICurrentUser _user;

    public ProjectsService(IProjectsRepository projectsRepository,
        IProjectMembershipsService projectMembershipsService,
        IBoardsService boardsService,
        ICurrentUser user, IAuthorizationGuard authGuard)
    {
        _projectsRepository = projectsRepository;
        _projectMembershipsService = projectMembershipsService;
        _boardsService = boardsService;
        _user = user;
        _authGuard = authGuard;
    }

    public Task<List<ProjectDto>> GetAll() => _projectsRepository.GetAllAsync();

    public Task<List<ProjectDto>> GetCurrentUserProjects() =>
        _projectsRepository.GetAllByUserIdAsync(_user.Id);

    public async Task<ProjectDto> GetById(int id) =>
        (await _projectsRepository.GetByIdAsync(id)).ToDto();

    public async Task<ProjectDto> Create(CreateProjectRequest request)
    {
        var project = new Project
        {
            OwnerId = _user.Id,
            Name = request.Name,
            Description = request.Description
        };

        _projectsRepository.Add(project);
        await _projectsRepository.SaveChangesAsync();

        await _projectMembershipsService.AddMember(project.Id, _user.Id, ProjectMemberRole.Lead);
        await _boardsService.CreateEmpty(project.Id);

        await _projectsRepository.LoadOwnerAsync(project);

        return project.ToDto();
    }

    public async Task<ProjectDto> Update(int id, UpdateProjectRequest request)
    {
        var project = await _projectsRepository.GetByIdAsync(id);
        await _authGuard.AuthorizeOwner(project);

        project.Name = request.Name;
        if (request.Description is not null) project.Description = request.Description;

        await _projectsRepository.SaveChangesAsync();

        return project.ToDto();
    }

    public async Task Delete(int id)
    {
        var project = await _projectsRepository.GetByIdAsync(id);
        await _authGuard.AuthorizeOwner(project);

        _projectsRepository.Remove(project);
        await _projectsRepository.SaveChangesAsync();
    }
}