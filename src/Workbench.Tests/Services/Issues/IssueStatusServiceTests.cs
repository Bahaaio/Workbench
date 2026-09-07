using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Dtos.Requests;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Issues.Services.Implementations;
using Workbench.Tests.Helpers;

namespace Workbench.Tests.Services.Issues;

public class IssueStatusServiceTests : IDisposable
{
    private const int CurrentUserId = 10;
    private const int IssueId = 100;

    private readonly Data.AppDbContext _db;
    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly IssueStatusService _service;

    public IssueStatusServiceTests()
    {
        _db = TestDbContextFactory.Create();

        var userMock = new Mock<ICurrentUser>();
        userMock.Setup(u => u.Id).Returns(CurrentUserId);

        _authGuard = new Mock<IAuthorizationGuard>();

        _service = new IssueStatusService(
            _db,
            userMock.Object,
            _authGuard.Object,
            Mock.Of<ILogger<IssueStatusService>>());
    }

    public void Dispose() => _db.Dispose();

    private async Task SeedIssue(Status status = Status.Open)
    {
        var author = new Modules.Auth.Models.ApplicationUser { Id = 99, UserName = "author" };
        var owner = new Modules.Auth.Models.ApplicationUser { Id = 1, UserName = "owner" };
        var currentUser = new Modules.Auth.Models.ApplicationUser { Id = CurrentUserId, UserName = "current" };
        _db.Users.AddRange(author, owner, currentUser);
        var project = new Modules.Projects.Models.Project { Id = 1, OwnerId = 1, Name = "P", Description = null };
        _db.Projects.Add(project);
        _db.Issues.Add(new Issue
        {
            Id = IssueId,
            ProjectId = 1,
            Title = "Issue",
            Status = status,
            AuthorId = 99,
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateStatus_UpdatesStatusAndRecordsChange()
    {
        await SeedIssue();

        await _service.UpdateStatus(IssueId, new UpdateIssueStatusRequest(Status.InProgress));

        var issue = await _db.Issues.FindAsync(IssueId);
        Assert.Equal(Status.InProgress, issue!.Status);
        Assert.Single(_db.IssueStatusChanges);
        var change = _db.IssueStatusChanges.First();
        Assert.Equal(IssueId, change.IssueId);
        Assert.Equal(Status.Open, change.FromStatus);
        Assert.Equal(Status.InProgress, change.ToStatus);
        Assert.Equal(CurrentUserId, change.ChangedByUserId);
    }

    [Fact]
    public async Task UpdateStatus_ShortCircuits_WhenStatusUnchanged()
    {
        await SeedIssue(status: Status.Open);

        await _service.UpdateStatus(IssueId, new UpdateIssueStatusRequest(Status.Open));

        Assert.Empty(_db.IssueStatusChanges);
    }

    [Fact]
    public async Task UpdateStatus_Throws_WhenNotProjectMember()
    {
        await SeedIssue();
        _authGuard.Setup(g => g.Authorize(It.IsAny<Issue>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not member"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.UpdateStatus(IssueId, new UpdateIssueStatusRequest(Status.Closed)));
    }

    [Fact]
    public async Task UpdateStatus_AllowsClosingFromOpen()
    {
        await SeedIssue(status: Status.Open);

        await _service.UpdateStatus(IssueId, new UpdateIssueStatusRequest(Status.Closed));

        var issue = await _db.Issues.FindAsync(IssueId);
        Assert.Equal(Status.Closed, issue!.Status);
    }

    [Fact]
    public async Task UpdateStatus_AllowsReopeningFromClosed()
    {
        await SeedIssue(status: Status.Closed);

        await _service.UpdateStatus(IssueId, new UpdateIssueStatusRequest(Status.Open));

        var issue = await _db.Issues.FindAsync(IssueId);
        Assert.Equal(Status.Open, issue!.Status);
        var change = _db.IssueStatusChanges.First();
        Assert.Equal(Status.Closed, change.FromStatus);
        Assert.Equal(Status.Open, change.ToStatus);
    }

    [Fact]
    public async Task UpdateStatus_AllowsTransitionFromInProgress()
    {
        await SeedIssue(status: Status.InProgress);

        await _service.UpdateStatus(IssueId, new UpdateIssueStatusRequest(Status.Closed));

        var issue = await _db.Issues.FindAsync(IssueId);
        Assert.Equal(Status.Closed, issue!.Status);
    }

    [Fact]
    public async Task GetStatusHistory_Throws_WhenIssueNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetStatusHistory(IssueId));
    }
}
