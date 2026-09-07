using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Dtos.Requests;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Issues.Repositories;
using Workbench.Modules.Issues.Services.Implementations;

namespace Workbench.Tests.Services.Issues;

public class IssueStatusServiceTests
{
    private const int CurrentUserId = 10;
    private const int IssueId = 100;

    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly Mock<IIssuesRepository> _issuesRepo;
    private readonly Mock<IIssueStatusChangeRepository> _statusChangeRepo;
    private readonly IssueStatusService _service;

    public IssueStatusServiceTests()
    {
        var userMock = new Mock<ICurrentUser>();
        userMock.Setup(u => u.Id).Returns(CurrentUserId);

        _authGuard = new Mock<IAuthorizationGuard>();
        _issuesRepo = new Mock<IIssuesRepository>();
        _statusChangeRepo = new Mock<IIssueStatusChangeRepository>();

        _service = new IssueStatusService(
            _issuesRepo.Object,
            _statusChangeRepo.Object,
            userMock.Object,
            _authGuard.Object,
            Mock.Of<ILogger<IssueStatusService>>());
    }

    private static Issue MakeIssue(Status status = Status.Open) =>
        new()
        {
            Id = IssueId,
            ProjectId = 1,
            Title = "Issue",
            Status = status,
            AuthorId = 99,
            Author = new Modules.Auth.Models.ApplicationUser { Id = 99, UserName = "author" }
        };

    [Fact]
    public async Task UpdateStatus_UpdatesStatusAndRecordsChange()
    {
        var issue = MakeIssue();
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        await _service.UpdateStatus(IssueId, new UpdateIssueStatusRequest(Status.InProgress));

        Assert.Equal(Status.InProgress, issue.Status);
        _statusChangeRepo.Verify(r => r.Add(It.Is<IssueStatusChange>(sc =>
            sc.IssueId == IssueId &&
            sc.FromStatus == Status.Open &&
            sc.ToStatus == Status.InProgress &&
            sc.ChangedByUserId == CurrentUserId)), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_ShortCircuits_WhenStatusUnchanged()
    {
        var issue = MakeIssue(status: Status.Open);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        await _service.UpdateStatus(IssueId, new UpdateIssueStatusRequest(Status.Open));

        _statusChangeRepo.Verify(r => r.Add(It.IsAny<IssueStatusChange>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_Throws_WhenNotProjectMember()
    {
        var issue = MakeIssue();
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);
        _authGuard.Setup(g => g.Authorize(It.IsAny<Issue>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not member"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.UpdateStatus(IssueId, new UpdateIssueStatusRequest(Status.Closed)));

    }

    [Fact]
    public async Task UpdateStatus_AllowsClosingFromOpen()
    {
        var issue = MakeIssue(status: Status.Open);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        await _service.UpdateStatus(IssueId, new UpdateIssueStatusRequest(Status.Closed));

        Assert.Equal(Status.Closed, issue.Status);
    }

    [Fact]
    public async Task UpdateStatus_AllowsReopeningFromClosed()
    {
        var issue = MakeIssue(status: Status.Closed);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        await _service.UpdateStatus(IssueId, new UpdateIssueStatusRequest(Status.Open));

        Assert.Equal(Status.Open, issue.Status);
        _statusChangeRepo.Verify(r => r.Add(It.Is<IssueStatusChange>(sc =>
            sc.FromStatus == Status.Closed &&
            sc.ToStatus == Status.Open)), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_AllowsTransitionFromInProgress()
    {
        var issue = MakeIssue(status: Status.InProgress);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        await _service.UpdateStatus(IssueId, new UpdateIssueStatusRequest(Status.Closed));

        Assert.Equal(Status.Closed, issue.Status);
    }

    [Fact]
    public async Task GetStatusHistory_ReturnsHistory_WhenIssueExists()
    {
        _issuesRepo.Setup(r => r.ExistsOrThrowAsync(IssueId)).Returns(Task.CompletedTask);
        var history = new List<Workbench.Modules.Issues.Dtos.StatusChangeDto>
        {
            new() { FromStatus = Status.Open, ToStatus = Status.InProgress, ChangedByUsername = "u", ChangedAt = DateTime.UtcNow }
        };
        _statusChangeRepo.Setup(r => r.GetHistoryAsync(IssueId)).ReturnsAsync(history);

        var result = await _service.GetStatusHistory(IssueId);

        Assert.Single(result);
        Assert.Equal(Status.Open, result[0].FromStatus);
        Assert.Equal(Status.InProgress, result[0].ToStatus);
    }

    [Fact]
    public async Task GetStatusHistory_Throws_WhenIssueNotFound()
    {
        _issuesRepo.Setup(r => r.ExistsOrThrowAsync(IssueId))
            .ThrowsAsync(new NotFoundException("Not found"));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetStatusHistory(IssueId));
    }
}
