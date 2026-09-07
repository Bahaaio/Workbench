using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Auth.Models;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Issues.Services.Implementations;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Models;
using Workbench.Modules.Projects.Memberships.Dtos;
using Workbench.Modules.Projects.Memberships.Services;
using Workbench.Tests.Helpers;

namespace Workbench.Tests.Services.Issues;

public class IssueAssignmentsServiceTests : IDisposable
{
    private const int CurrentUserId = 10;
    private const int OtherUserId = 20;
    private const int ProjectId = 1;
    private const int IssueId = 100;

    private readonly Data.AppDbContext _db;
    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly Mock<IProjectMembershipsService> _membershipsService;
    private readonly IssueAssignmentsService _service;

    public IssueAssignmentsServiceTests()
    {
        _db = TestDbContextFactory.Create();

        var userMock = new Mock<ICurrentUser>();
        userMock.Setup(u => u.Id).Returns(CurrentUserId);

        _authGuard = new Mock<IAuthorizationGuard>();
        _membershipsService = new Mock<IProjectMembershipsService>();

        _service = new IssueAssignmentsService(
            _db,
            userMock.Object,
            _authGuard.Object,
            _membershipsService.Object);
    }

    public void Dispose() => _db.Dispose();

    private async Task SeedIssue(int? assignedToId = null, Status status = Status.Open)
    {
        var author = new ApplicationUser { Id = 99, UserName = "author" };
        var owner = new ApplicationUser { Id = 1, UserName = "owner" };
        var currentUser = new ApplicationUser { Id = CurrentUserId, UserName = "current" };
        var otherUser = new ApplicationUser { Id = OtherUserId, UserName = "other" };
        _db.Users.AddRange(author, owner, currentUser, otherUser);
        var project = new Project { Id = ProjectId, OwnerId = 1, Name = "P", Description = null, Visibility = ProjectVisibility.Public };
        _db.Projects.Add(project);
        _db.Issues.Add(new Issue
        {
            Id = IssueId,
            ProjectId = ProjectId,
            Title = "Test Issue",
            Status = status,
            AuthorId = 99,
            AssignedToId = assignedToId,
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task AssignCurrentUser_SetsAssignedTo_WhenOpenAndUnassigned()
    {
        await SeedIssue();

        await _service.AssignCurrentUser(IssueId);

        var issue = await _db.Issues.FindAsync(IssueId);
        Assert.Equal(CurrentUserId, issue!.AssignedToId);
    }

    [Fact]
    public async Task AssignCurrentUser_Throws_WhenIssueClosed()
    {
        await SeedIssue(status: Status.Closed);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.AssignCurrentUser(IssueId));
    }

    [Fact]
    public async Task AssignCurrentUser_Throws_WhenAlreadyAssigned()
    {
        await SeedIssue(assignedToId: OtherUserId);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.AssignCurrentUser(IssueId));
    }

    [Fact]
    public async Task UnassignCurrentUser_ClearsAssignedTo_WhenAssignedToCurrentUser()
    {
        await SeedIssue(assignedToId: CurrentUserId);

        await _service.UnassignCurrentUser(IssueId);

        var issue = await _db.Issues.FindAsync(IssueId);
        Assert.Null(issue!.AssignedToId);
    }

    [Fact]
    public async Task UnassignCurrentUser_Throws_WhenNotAssignedToCurrentUser()
    {
        await SeedIssue(assignedToId: OtherUserId);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.UnassignCurrentUser(IssueId));
    }

    [Fact]
    public async Task UnassignCurrentUser_Throws_WhenIssueClosed()
    {
        await SeedIssue(assignedToId: CurrentUserId, status: Status.Closed);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.UnassignCurrentUser(IssueId));
    }

    [Fact]
    public async Task AssignUser_SetsAssignedToByUserId()
    {
        await SeedIssue();
        _membershipsService.Setup(s => s.GetProjectMembership(ProjectId, "targetuser"))
            .ReturnsAsync(new ProjectMembershipDto(OtherUserId, "targetuser", ProjectMemberRole.Member));

        await _service.AssignUser(IssueId, "targetuser");

        var issue = await _db.Issues.FindAsync(IssueId);
        Assert.Equal(OtherUserId, issue!.AssignedToId);
    }

    [Fact]
    public async Task AssignUser_Throws_WhenIssueClosed()
    {
        await SeedIssue(status: Status.Closed);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.AssignUser(IssueId, "targetuser"));
    }

    [Fact]
    public async Task UnassignUser_ClearsAssignedTo()
    {
        await SeedIssue(assignedToId: OtherUserId);

        await _service.UnassignUser(IssueId);

        var issue = await _db.Issues.FindAsync(IssueId);
        Assert.Null(issue!.AssignedToId);
    }

    [Fact]
    public async Task UnassignUser_Throws_WhenIssueClosed()
    {
        await SeedIssue(assignedToId: OtherUserId, status: Status.Closed);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.UnassignUser(IssueId));
    }
}
