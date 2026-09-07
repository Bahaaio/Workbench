using Microsoft.AspNetCore.Authorization;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Auth.Models;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Kanban.Services;
using Workbench.Modules.Projects.Dtos.Requests;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Models;
using Workbench.Modules.Projects.Memberships.Services;
using Workbench.Modules.Projects.Services.Implementations;
using Workbench.Tests.Helpers;

namespace Workbench.Tests.Services.Projects;

public class ProjectsServiceTests : IDisposable
{
    private const int CurrentUserId = 10;
    private const int OtherUserId = 20;
    private const int ProjectId = 1;

    private readonly Data.AppDbContext _db;
    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly Mock<IBoardsService> _boardsService;
    private readonly Mock<IProjectMembershipsService> _membershipsService;
    private readonly ProjectsService _service;

    public ProjectsServiceTests()
    {
        _db = TestDbContextFactory.Create();

        var userMock = new Mock<ICurrentUser>();
        userMock.Setup(u => u.Id).Returns(CurrentUserId);

        _authGuard = new Mock<IAuthorizationGuard>();
        _boardsService = new Mock<IBoardsService>();
        _membershipsService = new Mock<IProjectMembershipsService>();

        _service = new ProjectsService(
            _db,
            _membershipsService.Object,
            _boardsService.Object,
            userMock.Object,
            _authGuard.Object);
    }

    public void Dispose() => _db.Dispose();

    private async Task SeedProject(int ownerId = OtherUserId, string name = "Test Project")
    {
        var users = new List<ApplicationUser>
        {
            new() { Id = ownerId, UserName = $"user{ownerId}" },
            new() { Id = CurrentUserId, UserName = "current" },
            new() { Id = OtherUserId, UserName = "other" },
        };
        var distinctUsers = users.GroupBy(u => u.Id).Select(g => g.First()).ToList();
        _db.Users.AddRange(distinctUsers);
        _db.Projects.Add(new Project
        {
            Id = ProjectId,
            OwnerId = ownerId,
            Name = name,
            Description = "Description",
            Visibility = ProjectVisibility.Public,
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetById_ReturnsProjectDto()
    {
        await SeedProject();

        var result = await _service.GetById(ProjectId);

        Assert.Equal(ProjectId, result.Id);
        Assert.Equal("Test Project", result.Name);
    }

    [Fact]
    public async Task GetById_Throws_WhenNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetById(ProjectId));
    }

    [Fact]
    public async Task Create_CreatesProjectAndBoardAndMembership()
    {
        _db.Users.Add(new ApplicationUser { Id = CurrentUserId, UserName = "current" });
        await _db.SaveChangesAsync();

        _membershipsService.Setup(s => s.AddMember(It.IsAny<int>(), CurrentUserId, ProjectMemberRole.Lead))
            .Returns(Task.CompletedTask);
        _boardsService.Setup(s => s.CreateEmpty(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var result = await _service.Create(new CreateProjectRequest
        {
            Name = "New Project",
            Description = "Desc"
        });

        Assert.Equal("New Project", result.Name);
        _membershipsService.Verify(s => s.AddMember(result.Id, CurrentUserId, ProjectMemberRole.Lead), Times.Once);
        _boardsService.Verify(s => s.CreateEmpty(result.Id), Times.Once);
    }

    [Fact]
    public async Task Create_SetsOwnerIdToCurrentUser()
    {
        _db.Users.Add(new ApplicationUser { Id = CurrentUserId, UserName = "current" });
        await _db.SaveChangesAsync();

        _membershipsService.Setup(s => s.AddMember(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ProjectMemberRole>()))
            .Returns(Task.CompletedTask);
        _boardsService.Setup(s => s.CreateEmpty(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var result = await _service.Create(new CreateProjectRequest { Name = "P" });

        var project = await _db.Projects.FindAsync(result.Id);
        Assert.Equal(CurrentUserId, project!.OwnerId);
    }

    [Fact]
    public async Task Update_UpdatesNameAndDescription_WhenAuthorized()
    {
        await SeedProject(ownerId: CurrentUserId);

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
        await SeedProject(ownerId: CurrentUserId);

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
        await SeedProject(ownerId: OtherUserId);
        _authGuard.Setup(g => g.Authorize(It.IsAny<Project>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not owner"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Update(ProjectId, new UpdateProjectRequest { Name = "X" }));
    }

    [Fact]
    public async Task Delete_RemovesProject_WhenAuthorized()
    {
        await SeedProject(ownerId: CurrentUserId);

        await _service.Delete(ProjectId);

        Assert.Null(await _db.Projects.FindAsync(ProjectId));
    }

    [Fact]
    public async Task Delete_Throws_WhenNotOwner()
    {
        await SeedProject(ownerId: OtherUserId);
        _authGuard.Setup(g => g.Authorize(It.IsAny<Project>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not owner"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Delete(ProjectId));

        Assert.NotNull(await _db.Projects.FindAsync(ProjectId));
    }
}
