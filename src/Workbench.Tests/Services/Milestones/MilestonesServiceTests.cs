using Microsoft.AspNetCore.Authorization;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Milestones.Dtos.Requests;
using Workbench.Modules.Milestones.Models;
using Workbench.Modules.Milestones.Repositories;
using Workbench.Modules.Milestones.Services.Implementations;
using Workbench.Modules.Projects.Models;
using Workbench.Modules.Projects.Repositories;

namespace Workbench.Tests.Services.Milestones;

public class MilestonesServiceTests
{
    private const int ProjectId = 1;
    private const int MilestoneId = 10;

    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly Mock<IMilestonesRepository> _milestonesRepo;
    private readonly Mock<IProjectsRepository> _projectsRepo;
    private readonly MilestonesService _service;

    public MilestonesServiceTests()
    {
        _authGuard = new Mock<IAuthorizationGuard>();
        _milestonesRepo = new Mock<IMilestonesRepository>();
        _projectsRepo = new Mock<IProjectsRepository>();

        _service = new MilestonesService(
            _milestonesRepo.Object,
            _projectsRepo.Object,
            _authGuard.Object);
    }

    private static Milestone MakeMilestone(int projectId = ProjectId) =>
        new()
        {
            Id = MilestoneId,
            ProjectId = projectId,
            Name = "Milestone 1",
            Description = "Desc",
            DueDate = null,
            Project = new Project
            {
                Id = projectId,
                OwnerId = 1,
                Name = "Project",
                Description = null,
                Owner = new Modules.Auth.Models.ApplicationUser { Id = 1, UserName = "owner" }
            }
        };

    [Fact]
    public async Task GetAll_ReturnsMilestones_WhenProjectExists()
    {
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _milestonesRepo.Setup(r => r.GetAllAsync(ProjectId))
            .ReturnsAsync(new List<Modules.Milestones.Dtos.MilestoneDto>());

        var result = await _service.GetAll(ProjectId);

        Assert.Empty(result);
        _projectsRepo.Verify(r => r.ExistsOrThrowAsync(ProjectId), Times.Once);
    }

    [Fact]
    public async Task GetAll_Throws_WhenProjectNotFound()
    {
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId))
            .ThrowsAsync(new NotFoundException("Project not found"));

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAll(ProjectId));
    }

    [Fact]
    public async Task GetById_ReturnsMilestone_WhenInProject()
    {
        var milestone = MakeMilestone();
        _milestonesRepo.Setup(r => r.GetByIdAsync(MilestoneId)).ReturnsAsync(milestone);

        var result = await _service.GetById(ProjectId, MilestoneId);

        Assert.Equal(MilestoneId, result.Id);
        Assert.Equal("Milestone 1", result.Name);
    }

    [Fact]
    public async Task GetById_Throws_WhenMilestoneNotInProject()
    {
        var milestone = MakeMilestone(projectId: 99);
        _milestonesRepo.Setup(r => r.GetByIdAsync(MilestoneId)).ReturnsAsync(milestone);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetById(ProjectId, MilestoneId));
    }

    [Fact]
    public async Task Create_CreatesMilestone_WhenAuthorized()
    {
        var project = new Project
        {
            Id = ProjectId,
            OwnerId = 1,
            Name = "P",
            Description = null,
            Owner = new Modules.Auth.Models.ApplicationUser { Id = 1, UserName = "u" }
        };
        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);

        _milestonesRepo.Setup(r => r.Add(It.IsAny<Milestone>()))
            .Callback<Milestone>(m => m.Id = MilestoneId);

        var result = await _service.Create(ProjectId, new CreateMilestoneRequest
        {
            Name = "New",
            Description = "Desc",
            DueDate = DateTime.UtcNow
        });

        Assert.Equal(MilestoneId, result.Id);
        _milestonesRepo.Verify(r => r.Add(It.IsAny<Milestone>()), Times.Once);
    }

    [Fact]
    public async Task Create_Throws_WhenNotProjectLead()
    {
        var project = new Project
        {
            Id = ProjectId,
            OwnerId = 1,
            Name = "P",
            Description = null,
            Owner = new Modules.Auth.Models.ApplicationUser { Id = 1, UserName = "u" }
        };
        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);
        _authGuard.Setup(g => g.Authorize(It.IsAny<Project>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Create(ProjectId, new CreateMilestoneRequest { Name = "X" }));

        _milestonesRepo.Verify(r => r.Add(It.IsAny<Milestone>()), Times.Never);
    }

    [Fact]
    public async Task Update_UpdatesFields_WhenAuthorized()
    {
        var milestone = MakeMilestone();
        _milestonesRepo.Setup(r => r.GetByIdAsync(MilestoneId)).ReturnsAsync(milestone);

        var result = await _service.Update(ProjectId, MilestoneId, new UpdateMilestoneRequest
        {
            Name = "Updated",
            Description = "New",
            DueDate = DateTime.UtcNow
        });

        Assert.Equal("Updated", result.Name);
        Assert.Equal("New", result.Description);
    }

    [Fact]
    public async Task Update_Throws_WhenMilestoneNotInProject()
    {
        var milestone = MakeMilestone(projectId: 99);
        _milestonesRepo.Setup(r => r.GetByIdAsync(MilestoneId)).ReturnsAsync(milestone);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Update(ProjectId, MilestoneId, new UpdateMilestoneRequest { Name = "X" }));

    }

    [Fact]
    public async Task Update_Throws_WhenNotProjectLead()
    {
        var milestone = MakeMilestone();
        _milestonesRepo.Setup(r => r.GetByIdAsync(MilestoneId)).ReturnsAsync(milestone);
        _authGuard.Setup(g => g.Authorize(It.IsAny<Milestone>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Update(ProjectId, MilestoneId, new UpdateMilestoneRequest { Name = "X" }));

    }

    [Fact]
    public async Task Delete_RemovesMilestone_WhenAuthorized()
    {
        var milestone = MakeMilestone();
        _milestonesRepo.Setup(r => r.GetByIdAsync(MilestoneId)).ReturnsAsync(milestone);

        await _service.Delete(ProjectId, MilestoneId);

        _milestonesRepo.Verify(r => r.Remove(milestone), Times.Once);
    }

    [Fact]
    public async Task Delete_Throws_WhenMilestoneNotInProject()
    {
        var milestone = MakeMilestone(projectId: 99);
        _milestonesRepo.Setup(r => r.GetByIdAsync(MilestoneId)).ReturnsAsync(milestone);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Delete(ProjectId, MilestoneId));

        _milestonesRepo.Verify(r => r.Remove(It.IsAny<Milestone>()), Times.Never);
    }

    [Fact]
    public async Task Delete_Throws_WhenNotProjectLead()
    {
        var milestone = MakeMilestone();
        _milestonesRepo.Setup(r => r.GetByIdAsync(MilestoneId)).ReturnsAsync(milestone);
        _authGuard.Setup(g => g.Authorize(It.IsAny<Milestone>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Delete(ProjectId, MilestoneId));

        _milestonesRepo.Verify(r => r.Remove(It.IsAny<Milestone>()), Times.Never);
    }
}
