using Microsoft.AspNetCore.Authorization;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Auth.Models;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Milestones.Dtos.Requests;
using Workbench.Modules.Milestones.Services.Implementations;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Models;
using Workbench.Tests.Helpers;

namespace Workbench.Tests.Services.Milestones;

public class MilestonesServiceTests : IDisposable
{
    private const int ProjectId = 1;
    private const int MilestoneId = 10;

    private readonly Data.AppDbContext _db;
    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly MilestonesService _service;

    public MilestonesServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _authGuard = new Mock<IAuthorizationGuard>();
        _service = new MilestonesService(_db, _authGuard.Object);
    }

    public void Dispose() => _db.Dispose();

    private async Task SeedProject(int ownerId = 1)
    {
        var owner = new ApplicationUser { Id = ownerId, UserName = "owner" };
        _db.Users.Add(owner);
        _db.Projects.Add(new Project
        {
            Id = ProjectId,
            OwnerId = ownerId,
            Name = "P",
            Description = null,
            Visibility = ProjectVisibility.Public,
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAll_ReturnsMilestones_WhenProjectExists()
    {
        await SeedProject();

        var result = await _service.GetAll(ProjectId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAll_Throws_WhenProjectNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAll(ProjectId));
    }

    [Fact]
    public async Task GetById_Throws_WhenMilestoneNotFound()
    {
        await SeedProject();

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetById(ProjectId, MilestoneId));
    }

    [Fact]
    public async Task Create_CreatesMilestone_WhenAuthorized()
    {
        await SeedProject();

        var result = await _service.Create(ProjectId, new CreateMilestoneRequest
        {
            Name = "New",
            Description = "Desc",
            DueDate = DateTime.UtcNow
        });

        Assert.Equal("New", result.Name);
        Assert.Equal("Desc", result.Description);
        Assert.NotNull(_db.Milestones.Find(result.Id));
    }

    [Fact]
    public async Task Create_Throws_WhenNotProjectLead()
    {
        await SeedProject();
        _authGuard.Setup(g => g.Authorize(It.IsAny<Project>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Create(ProjectId, new CreateMilestoneRequest { Name = "X" }));
    }

    [Fact]
    public async Task Update_UpdatesFields_WhenAuthorized()
    {
        await SeedProject();
        var milestone = new Modules.Milestones.Models.Milestone
        {
            Id = MilestoneId,
            ProjectId = ProjectId,
            Name = "Milestone 1",
            Description = "Desc",
            DueDate = null,
        };
        _db.Milestones.Add(milestone);
        await _db.SaveChangesAsync();

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
        await SeedProject();
        var otherOwner = new ApplicationUser { Id = 99, UserName = "other" };
        _db.Users.Add(otherOwner);
        _db.Projects.Add(new Project
        {
            Id = 99,
            OwnerId = 99,
            Name = "Other",
            Description = null,
            Visibility = ProjectVisibility.Public,
        });
        var milestone = new Modules.Milestones.Models.Milestone
        {
            Id = MilestoneId,
            ProjectId = 99,
            Name = "Milestone 1",
            Description = null,
            DueDate = null,
        };
        _db.Milestones.Add(milestone);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Update(ProjectId, MilestoneId, new UpdateMilestoneRequest { Name = "X" }));
    }

    [Fact]
    public async Task Delete_RemovesMilestone_WhenAuthorized()
    {
        await SeedProject();
        var milestone = new Modules.Milestones.Models.Milestone
        {
            Id = MilestoneId,
            ProjectId = ProjectId,
            Name = "Milestone 1",
            Description = null,
            DueDate = null,
        };
        _db.Milestones.Add(milestone);
        await _db.SaveChangesAsync();

        await _service.Delete(ProjectId, MilestoneId);

        Assert.Null(_db.Milestones.Find(MilestoneId));
    }

    [Fact]
    public async Task Delete_Throws_WhenMilestoneNotInProject()
    {
        await SeedProject();
        var otherOwner = new ApplicationUser { Id = 99, UserName = "other" };
        _db.Users.Add(otherOwner);
        _db.Projects.Add(new Project
        {
            Id = 99,
            OwnerId = 99,
            Name = "Other",
            Description = null,
            Visibility = ProjectVisibility.Public,
        });
        var milestone = new Modules.Milestones.Models.Milestone
        {
            Id = MilestoneId,
            ProjectId = 99,
            Name = "Milestone 1",
            Description = null,
            DueDate = null,
        };
        _db.Milestones.Add(milestone);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Delete(ProjectId, MilestoneId));

        Assert.NotNull(_db.Milestones.Find(MilestoneId));
    }

    [Fact]
    public async Task Delete_Throws_WhenNotProjectLead()
    {
        await SeedProject();
        var milestone = new Modules.Milestones.Models.Milestone
        {
            Id = MilestoneId,
            ProjectId = ProjectId,
            Name = "Milestone 1",
            Description = null,
            DueDate = null,
        };
        _db.Milestones.Add(milestone);
        await _db.SaveChangesAsync();

        _authGuard.Setup(g => g.Authorize(It.IsAny<Modules.Milestones.Models.Milestone>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Delete(ProjectId, MilestoneId));

        Assert.NotNull(_db.Milestones.Find(MilestoneId));
    }
}
