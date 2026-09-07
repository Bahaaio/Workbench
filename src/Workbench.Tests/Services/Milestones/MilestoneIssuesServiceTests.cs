using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Auth.Models;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Milestones.Models;
using Workbench.Modules.Milestones.Services.Implementations;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Models;
using Workbench.Tests.Helpers;

namespace Workbench.Tests.Services.Milestones;

public class MilestoneIssuesServiceTests : IDisposable
{
    private const int ProjectId = 1;
    private const int MilestoneId = 10;
    private const int IssueId = 100;

    private readonly Data.AppDbContext _db;
    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly MilestoneIssuesService _service;

    public MilestoneIssuesServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _authGuard = new Mock<IAuthorizationGuard>();
        _service = new MilestoneIssuesService(_db, _authGuard.Object);
    }

    public void Dispose() => _db.Dispose();

    private async Task SeedMilestone(int projectId = ProjectId)
    {
        var owner = new ApplicationUser { Id = 1, UserName = "owner" };
        _db.Users.Add(owner);
        _db.Projects.Add(new Project
        {
            Id = projectId,
            OwnerId = 1,
            Name = "P",
            Description = null,
            Visibility = ProjectVisibility.Public,
        });
        _db.Milestones.Add(new Milestone
        {
            Id = MilestoneId,
            ProjectId = projectId,
            Name = "M1",
            Description = null,
            DueDate = null,
        });
        await _db.SaveChangesAsync();
    }

    private async Task SeedIssue(int projectId = ProjectId)
    {
        var author = new ApplicationUser { Id = 99, UserName = "author" };
        _db.Users.Add(author);
        if (projectId != ProjectId)
        {
            if (!_db.ChangeTracker.Entries<ApplicationUser>().Any(e => e.Entity.Id == 1))
            {
                var otherOwner = new ApplicationUser { Id = 1, UserName = "owner" };
                _db.Users.Add(otherOwner);
            }
            if (!_db.ChangeTracker.Entries<Project>().Any(e => e.Entity.Id == projectId))
            {
                _db.Projects.Add(new Project
                {
                    Id = projectId,
                    OwnerId = 1,
                    Name = "P",
                    Description = null,
                    Visibility = ProjectVisibility.Public,
                });
            }
        }
        _db.Issues.Add(new Issue
        {
            Id = IssueId,
            ProjectId = projectId,
            Title = "Issue",
            AuthorId = 99,
            Status = Status.Open,
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllIssues_ReturnsIssues_WhenMilestoneInProject()
    {
        await SeedMilestone();

        var result = await _service.GetAllIssues(ProjectId, MilestoneId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllIssues_Throws_WhenMilestoneNotInProject()
    {
        await SeedMilestone(projectId: 99);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetAllIssues(ProjectId, MilestoneId));
    }

    [Fact]
    public async Task AddIssue_AddsItem_WhenValid()
    {
        await SeedMilestone();
        await SeedIssue();

        await _service.AddIssue(ProjectId, MilestoneId, IssueId);

        var items = _db.Milestones
            .Include(m => m.MilestoneItems)
            .First(m => m.Id == MilestoneId)
            .MilestoneItems;
        Assert.Single(items);
        Assert.Equal(IssueId, items.First().IssueId);
    }

    [Fact]
    public async Task AddIssue_Throws_WhenMilestoneNotInProject()
    {
        await SeedMilestone(projectId: 99);
        await SeedIssue(projectId: 99);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.AddIssue(ProjectId, MilestoneId, IssueId));
    }

    [Fact]
    public async Task AddIssue_Throws_WhenIssueNotInProject()
    {
        await SeedMilestone();
        await SeedIssue(projectId: 99);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.AddIssue(ProjectId, MilestoneId, IssueId));
    }

    [Fact]
    public async Task AddIssue_Throws_WhenDuplicateIssue()
    {
        await SeedMilestone();
        await SeedIssue();
        _db.MilestoneItems.Add(new MilestoneItem { MilestoneId = MilestoneId, IssueId = IssueId });
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.AddIssue(ProjectId, MilestoneId, IssueId));
    }

    [Fact]
    public async Task AddIssue_Throws_WhenNotProjectLead()
    {
        await SeedMilestone();
        await SeedIssue();
        _authGuard.Setup(g => g.Authorize(It.IsAny<Milestone>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.AddIssue(ProjectId, MilestoneId, IssueId));
    }

    [Fact]
    public async Task RemoveIssue_RemovesItem_WhenValid()
    {
        await SeedMilestone();
        await SeedIssue();
        _db.MilestoneItems.Add(new MilestoneItem { MilestoneId = MilestoneId, IssueId = IssueId });
        await _db.SaveChangesAsync();

        await _service.RemoveIssue(ProjectId, MilestoneId, IssueId);

        var items = _db.Milestones
            .Include(m => m.MilestoneItems)
            .First(m => m.Id == MilestoneId)
            .MilestoneItems;
        Assert.Empty(items);
    }

    [Fact]
    public async Task RemoveIssue_Throws_WhenIssueNotInMilestone()
    {
        await SeedMilestone();

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.RemoveIssue(ProjectId, MilestoneId, IssueId));
    }

    [Fact]
    public async Task RemoveIssue_Throws_WhenNotProjectLead()
    {
        await SeedMilestone();
        await SeedIssue();
        _db.MilestoneItems.Add(new MilestoneItem { MilestoneId = MilestoneId, IssueId = IssueId });
        await _db.SaveChangesAsync();

        _authGuard.Setup(g => g.Authorize(It.IsAny<Milestone>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.RemoveIssue(ProjectId, MilestoneId, IssueId));
    }
}
