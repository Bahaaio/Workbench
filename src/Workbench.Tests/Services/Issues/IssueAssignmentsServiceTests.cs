using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Requirements;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Issues.Repositories;
using Workbench.Modules.Issues.Services.Implementations;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Memberships.Dtos;
using Workbench.Modules.Projects.Memberships.Services;

namespace Workbench.Tests.Services.Issues;

public class IssueAssignmentsServiceTests
{
    private const int CurrentUserId = 10;
    private const int OtherUserId = 20;
    private const int ProjectId = 1;
    private const int IssueId = 100;

    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly Mock<IIssuesRepository> _issuesRepo;
    private readonly Mock<IProjectMembershipsService> _membershipsService;
    private readonly IssueAssignmentsService _service;

    public IssueAssignmentsServiceTests()
    {
        var userMock = new Mock<ICurrentUser>();
        userMock.Setup(u => u.Id).Returns(CurrentUserId);

        _authGuard = new Mock<IAuthorizationGuard>();
        _issuesRepo = new Mock<IIssuesRepository>();
        _membershipsService = new Mock<IProjectMembershipsService>();

        _service = new IssueAssignmentsService(
            _issuesRepo.Object,
            userMock.Object,
            _authGuard.Object,
            _membershipsService.Object);
    }

    private static Issue MakeIssue(int? assignedToId = null, Status status = Status.Open) =>
        new()
        {
            Id = IssueId,
            ProjectId = ProjectId,
            Title = "Test Issue",
            Status = status,
            AuthorId = 99,
            AssignedToId = assignedToId,
            Author = new Modules.Auth.Models.ApplicationUser { Id = 99, UserName = "author" }
        };

    [Fact]
    public async Task AssignCurrentUser_SetsAssignedTo_WhenOpenAndUnassigned()
    {
        var issue = MakeIssue();
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        await _service.AssignCurrentUser(IssueId);

        Assert.Equal(CurrentUserId, issue.AssignedToId);
    }

    [Fact]
    public async Task AssignCurrentUser_Throws_WhenIssueClosed()
    {
        var issue = MakeIssue(status: Status.Closed);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.AssignCurrentUser(IssueId));

    }

    [Fact]
    public async Task AssignCurrentUser_Throws_WhenAlreadyAssigned()
    {
        var issue = MakeIssue(assignedToId: OtherUserId);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.AssignCurrentUser(IssueId));

    }

    [Fact]
    public async Task UnassignCurrentUser_ClearsAssignedTo_WhenAssignedToCurrentUser()
    {
        var issue = MakeIssue(assignedToId: CurrentUserId);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        await _service.UnassignCurrentUser(IssueId);

        Assert.Null(issue.AssignedToId);
    }

    [Fact]
    public async Task UnassignCurrentUser_Throws_WhenNotAssignedToCurrentUser()
    {
        var issue = MakeIssue(assignedToId: OtherUserId);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.UnassignCurrentUser(IssueId));

    }

    [Fact]
    public async Task UnassignCurrentUser_Throws_WhenIssueClosed()
    {
        var issue = MakeIssue(assignedToId: CurrentUserId, status: Status.Closed);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.UnassignCurrentUser(IssueId));

    }

    [Fact]
    public async Task AssignUser_SetsAssignedToByUserId()
    {
        var issue = MakeIssue();
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);
        _membershipsService.Setup(s => s.GetProjectMembership(ProjectId, "targetuser"))
            .ReturnsAsync(new ProjectMembershipDto(OtherUserId, "targetuser", ProjectMemberRole.Member));

        await _service.AssignUser(IssueId, "targetuser");

        Assert.Equal(OtherUserId, issue.AssignedToId);
    }

    [Fact]
    public async Task AssignUser_Throws_WhenIssueClosed()
    {
        var issue = MakeIssue(status: Status.Closed);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.AssignUser(IssueId, "targetuser"));

    }

    [Fact]
    public async Task UnassignUser_ClearsAssignedTo()
    {
        var issue = MakeIssue(assignedToId: OtherUserId);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        await _service.UnassignUser(IssueId);

        Assert.Null(issue.AssignedToId);
    }

    [Fact]
    public async Task UnassignUser_Throws_WhenIssueClosed()
    {
        var issue = MakeIssue(assignedToId: OtherUserId, status: Status.Closed);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.UnassignUser(IssueId));

    }
}
