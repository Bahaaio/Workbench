using Microsoft.AspNetCore.Authorization;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Kanban.Services;
using Workbench.Modules.Projects.Dtos.Requests;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Mappers;
using Workbench.Modules.Projects.Memberships.Services;
using Workbench.Modules.Projects.Models;
using Workbench.Modules.Projects.Repositories;
using Workbench.Modules.Projects.Services.Implementations;

namespace Workbench.Tests.Services.Projects;

public class ProjectsServiceTests
{
    private const int CurrentUserId = 10;
    private const int OtherUserId = 20;
    private const int ProjectId = 1;

    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly Mock<IBoardsService> _boardsService;
    private readonly Mock<IProjectMembershipsService> _membershipsService;
    private readonly Mock<IProjectsRepository> _projectsRepo;
    private readonly ProjectsService _service;

    public ProjectsServiceTests()
    {
        var userMock = new Mock<ICurrentUser>();
        userMock.Setup(u => u.Id).Returns(CurrentUserId);

        _authGuard = new Mock<IAuthorizationGuard>();
        _boardsService = new Mock<IBoardsService>();
        _membershipsService = new Mock<IProjectMembershipsService>();
        _projectsRepo = new Mock<IProjectsRepository>();

        _service = new ProjectsService(
            _projectsRepo.Object,
            _membershipsService.Object,
            _boardsService.Object,
            userMock.Object,
            _authGuard.Object);
    }

    private static Project MakeProject(int ownerId = OtherUserId, string name = "Test Project") =>
        new()
        {
            Id = ProjectId,
            OwnerId = ownerId,
            Name = name,
            Description = "Description",
            Owner = new Modules.Auth.Models.ApplicationUser { Id = ownerId, UserName = "owner" }
        };

    [Fact]
    public async Task GetById_ReturnsProjectDto()
    {
        var project = MakeProject();
        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);

        var result = await _service.GetById(ProjectId);

        Assert.Equal(ProjectId, result.Id);
        Assert.Equal(project.Name, result.Name);
    }

    [Fact]
    public async Task GetById_Throws_WhenNotFound()
    {
        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId))
            .ThrowsAsync(new NotFoundException("not found"));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetById(ProjectId));
    }

    [Fact]
    public async Task Create_CreatesProjectAndBoardAndMembership()
    {
        _projectsRepo.Setup(r => r.Add(It.IsAny<Project>()))
            .Callback<Project>(p => p.Id = ProjectId);
        _membershipsService.Setup(s => s.AddMember(ProjectId, CurrentUserId, ProjectMemberRole.Lead))
            .Returns(Task.CompletedTask);
        _boardsService.Setup(s => s.CreateEmpty(ProjectId))
            .Returns(Task.CompletedTask);
        _projectsRepo.Setup(r => r.LoadOwnerAsync(It.IsAny<Project>()))
            .Callback<Project>(p => p.Owner = new Modules.Auth.Models.ApplicationUser { Id = CurrentUserId, UserName = "current" })
            .Returns(Task.CompletedTask);

        var result = await _service.Create(new CreateProjectRequest
        {
            Name = "New Project",
            Description = "Desc"
        });

        Assert.Equal(ProjectId, result.Id);
        Assert.Equal("New Project", result.Name);
        _membershipsService.Verify(s => s.AddMember(ProjectId, CurrentUserId, ProjectMemberRole.Lead), Times.Once);
        _boardsService.Verify(s => s.CreateEmpty(ProjectId), Times.Once);
    }

    [Fact]
    public async Task Create_SetsOwnerIdToCurrentUser()
    {
        Project? captured = null;
        _projectsRepo.Setup(r => r.Add(It.IsAny<Project>()))
            .Callback<Project>(p => { p.Id = ProjectId; captured = p; });
        _membershipsService.Setup(s => s.AddMember(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ProjectMemberRole>()))
            .Returns(Task.CompletedTask);
        _boardsService.Setup(s => s.CreateEmpty(It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        _projectsRepo.Setup(r => r.LoadOwnerAsync(It.IsAny<Project>()))
            .Callback<Project>(p => p.Owner = new Modules.Auth.Models.ApplicationUser { Id = CurrentUserId, UserName = "current" })
            .Returns(Task.CompletedTask);

        await _service.Create(new CreateProjectRequest { Name = "P" });

        Assert.Equal(CurrentUserId, captured!.OwnerId);
    }

    [Fact]
    public async Task Update_UpdatesNameAndDescription_WhenAuthorized()
    {
        var project = MakeProject(ownerId: CurrentUserId);
        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);

        var result = await _service.Update(ProjectId, new UpdateProjectRequest
        {
            Name = "Updated",
            Description = "New desc"
        });

        Assert.Equal("Updated", result.Name);
        Assert.Equal("New desc", result.Description);
    }

    [Fact]
    public async Task Update_PreservesDescription_WhenNull()
    {
        var project = MakeProject(ownerId: CurrentUserId);
        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);

        var result = await _service.Update(ProjectId, new UpdateProjectRequest
        {
            Name = "Updated",
            Description = null
        });

        Assert.Equal("Updated", result.Name);
        Assert.Equal("Description", result.Description);
    }

    [Fact]
    public async Task Update_Throws_WhenNotOwner()
    {
        var project = MakeProject(ownerId: OtherUserId);
        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);
        _authGuard.Setup(g => g.Authorize(It.IsAny<Project>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not owner"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Update(ProjectId, new UpdateProjectRequest { Name = "X" }));

    }

    [Fact]
    public async Task Delete_RemovesProject_WhenAuthorized()
    {
        var project = MakeProject(ownerId: CurrentUserId);
        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);

        await _service.Delete(ProjectId);

        _projectsRepo.Verify(r => r.Remove(project), Times.Once);
    }

    [Fact]
    public async Task Delete_Throws_WhenNotOwner()
    {
        var project = MakeProject(ownerId: OtherUserId);
        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);
        _authGuard.Setup(g => g.Authorize(It.IsAny<Project>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not owner"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Delete(ProjectId));

        _projectsRepo.Verify(r => r.Remove(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_DelegatesToRepository()
    {
        var projects = new List<Modules.Projects.Dtos.ProjectDto>
        {
            new() { Id = 1, Name = "A", Description = null, CreatedAt = DateTime.UtcNow, OwnerUsername = "u" }
        };
        _projectsRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(projects);

        var result = await _service.GetAll();

        Assert.Single(result);
        Assert.Equal("A", result[0].Name);
    }

    [Fact]
    public async Task GetCurrentUserProjects_DelegatesToRepository()
    {
        _projectsRepo.Setup(r => r.GetAllByUserIdAsync(CurrentUserId))
            .ReturnsAsync(new List<Modules.Projects.Dtos.ProjectDto>());

        var result = await _service.GetCurrentUserProjects();

        Assert.Empty(result);
    }
}
