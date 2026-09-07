using Microsoft.AspNetCore.Authorization;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Issues.Repositories;
using Workbench.Modules.Milestones.Models;
using Workbench.Modules.Milestones.Repositories;
using Workbench.Modules.Milestones.Services.Implementations;
using Workbench.Modules.Projects.Models;

namespace Workbench.Tests.Services.Milestones;

public class MilestoneIssuesServiceTests
{
    private const int ProjectId = 1;
    private const int MilestoneId = 10;
    private const int IssueId = 100;

    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly Mock<IMilestonesRepository> _milestonesRepo;
    private readonly Mock<IIssuesRepository> _issuesRepo;
    private readonly MilestoneIssuesService _service;

    public MilestoneIssuesServiceTests()
    {
        _authGuard = new Mock<IAuthorizationGuard>();
        _milestonesRepo = new Mock<IMilestonesRepository>();
        _issuesRepo = new Mock<IIssuesRepository>();

        _service = new MilestoneIssuesService(
            _milestonesRepo.Object,
            _issuesRepo.Object,
            _authGuard.Object);
    }

    private static Milestone MakeMilestone(int projectId = ProjectId, List<MilestoneItem>? items = null) =>
        new()
        {
            Id = MilestoneId,
            ProjectId = projectId,
            Name = "M1",
            Description = null,
            DueDate = null,
            MilestoneItems = items ?? [],
            Project = new Project
            {
                Id = projectId,
                OwnerId = 1,
                Name = "P",
                Description = null,
                Owner = new Modules.Auth.Models.ApplicationUser { Id = 1, UserName = "u" }
            }
        };

    private static Issue MakeIssue(int projectId = ProjectId) =>
        new()
        {
            Id = IssueId,
            ProjectId = projectId,
            Title = "Issue",
            AuthorId = 99,
            Author = new Modules.Auth.Models.ApplicationUser { Id = 99, UserName = "author" }
        };

    [Fact]
    public async Task GetAllIssues_ReturnsIssues_WhenMilestoneInProject()
    {
        var milestone = MakeMilestone();
        _milestonesRepo.Setup(r => r.GetByIdAsync(MilestoneId)).ReturnsAsync(milestone);
        _milestonesRepo.Setup(r => r.GetAllIssuesAsync(MilestoneId))
            .ReturnsAsync(new List<Workbench.Modules.Issues.Dtos.IssueDto>());

        var result = await _service.GetAllIssues(ProjectId, MilestoneId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllIssues_Throws_WhenMilestoneNotInProject()
    {
        var milestone = MakeMilestone(projectId: 99);
        _milestonesRepo.Setup(r => r.GetByIdAsync(MilestoneId)).ReturnsAsync(milestone);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetAllIssues(ProjectId, MilestoneId));
    }

    [Fact]
    public async Task AddIssue_AddsItem_WhenValid()
    {
        var milestone = MakeMilestone();
        _milestonesRepo.Setup(r => r.FindForUpdateAsync(MilestoneId)).ReturnsAsync(milestone);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(MakeIssue());

        await _service.AddIssue(ProjectId, MilestoneId, IssueId);

        Assert.Single(milestone.MilestoneItems);
        Assert.Equal(IssueId, milestone.MilestoneItems.First().IssueId);
    }

    [Fact]
    public async Task AddIssue_Throws_WhenMilestoneNotFound()
    {
        _milestonesRepo.Setup(r => r.FindForUpdateAsync(MilestoneId))
            .ThrowsAsync(new NotFoundException("Not found"));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.AddIssue(ProjectId, MilestoneId, IssueId));

    }

    [Fact]
    public async Task AddIssue_Throws_WhenMilestoneNotInProject()
    {
        var milestone = MakeMilestone(projectId: 99);
        _milestonesRepo.Setup(r => r.FindForUpdateAsync(MilestoneId)).ReturnsAsync(milestone);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.AddIssue(ProjectId, MilestoneId, IssueId));
    }

    [Fact]
    public async Task AddIssue_Throws_WhenIssueNotInProject()
    {
        var milestone = MakeMilestone();
        _milestonesRepo.Setup(r => r.FindForUpdateAsync(MilestoneId)).ReturnsAsync(milestone);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(MakeIssue(projectId: 99));

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.AddIssue(ProjectId, MilestoneId, IssueId));

    }

    [Fact]
    public async Task AddIssue_Throws_WhenDuplicateIssue()
    {
        var existingItem = new MilestoneItem { MilestoneId = MilestoneId, IssueId = IssueId };
        var milestone = MakeMilestone(items: [existingItem]);
        _milestonesRepo.Setup(r => r.FindForUpdateAsync(MilestoneId)).ReturnsAsync(milestone);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(MakeIssue());

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.AddIssue(ProjectId, MilestoneId, IssueId));

    }

    [Fact]
    public async Task AddIssue_Throws_WhenNotProjectLead()
    {
        var milestone = MakeMilestone();
        _milestonesRepo.Setup(r => r.FindForUpdateAsync(MilestoneId)).ReturnsAsync(milestone);
        _authGuard.Setup(g => g.Authorize(It.IsAny<Milestone>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.AddIssue(ProjectId, MilestoneId, IssueId));
    }

    [Fact]
    public async Task RemoveIssue_RemovesItem_WhenValid()
    {
        var item = new MilestoneItem { MilestoneId = MilestoneId, IssueId = IssueId };
        var milestone = MakeMilestone(items: [item]);
        _milestonesRepo.Setup(r => r.FindForUpdateAsync(MilestoneId)).ReturnsAsync(milestone);

        await _service.RemoveIssue(ProjectId, MilestoneId, IssueId);

        Assert.Empty(milestone.MilestoneItems);
    }

    [Fact]
    public async Task RemoveIssue_Throws_WhenIssueNotInMilestone()
    {
        var milestone = MakeMilestone(items: []);
        _milestonesRepo.Setup(r => r.FindForUpdateAsync(MilestoneId)).ReturnsAsync(milestone);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.RemoveIssue(ProjectId, MilestoneId, IssueId));

    }

    [Fact]
    public async Task RemoveIssue_Throws_WhenMilestoneNotInProject()
    {
        var milestone = MakeMilestone(projectId: 99);
        _milestonesRepo.Setup(r => r.FindForUpdateAsync(MilestoneId)).ReturnsAsync(milestone);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.RemoveIssue(ProjectId, MilestoneId, IssueId));
    }

    [Fact]
    public async Task RemoveIssue_Throws_WhenNotProjectLead()
    {
        var milestone = MakeMilestone();
        _milestonesRepo.Setup(r => r.FindForUpdateAsync(MilestoneId)).ReturnsAsync(milestone);
        _authGuard.Setup(g => g.Authorize(It.IsAny<Milestone>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.RemoveIssue(ProjectId, MilestoneId, IssueId));
    }
}
