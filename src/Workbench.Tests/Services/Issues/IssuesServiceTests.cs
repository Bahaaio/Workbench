using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Attachments.Services;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Dtos.Requests;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Issues.Services.Implementations;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Models;
using Workbench.Tests.Helpers;

namespace Workbench.Tests.Services.Issues;

public class IssuesServiceTests : IDisposable
{
    private const int CurrentUserId = 10;
    private const int ProjectId = 1;
    private const int IssueId = 100;

    private readonly Data.AppDbContext _db;
    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly Mock<IAttachmentsService<Issue>> _attachmentsService;
    private readonly IssuesService _service;

    public IssuesServiceTests()
    {
        _db = TestDbContextFactory.Create();

        var userMock = new Mock<ICurrentUser>();
        userMock.Setup(u => u.Id).Returns(CurrentUserId);

        _authGuard = new Mock<IAuthorizationGuard>();
        _attachmentsService = new Mock<IAttachmentsService<Issue>>();

        _service = new IssuesService(
            _db,
            userMock.Object,
            _authGuard.Object,
            Mock.Of<ILogger<IssuesService>>(),
            _attachmentsService.Object);
    }

    public void Dispose() => _db.Dispose();

    private async Task SeedProject(int projectId = ProjectId, int ownerId = 1)
    {
        var owner = new Modules.Auth.Models.ApplicationUser { Id = ownerId, UserName = "owner" };
        var currentUser = new Modules.Auth.Models.ApplicationUser { Id = CurrentUserId, UserName = "current" };
        var author = new Modules.Auth.Models.ApplicationUser { Id = 99, UserName = "author" };
        _db.Users.AddRange(owner, currentUser, author);
        _db.Projects.Add(new Project
        {
            Id = projectId,
            OwnerId = ownerId,
            Name = "P",
            Description = null,
            Visibility = ProjectVisibility.Public,
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Create_CreatesIssueWithAuthorId()
    {
        await SeedProject();

        var result = await _service.Create(ProjectId, new CreateIssueRequest
        {
            Title = "New Issue",
            Description = "Desc"
        });

        Assert.Equal("New Issue", result.Title);
        var issue = await _db.Issues.FindAsync(result.Id);
        Assert.NotNull(issue);
        Assert.Equal(CurrentUserId, issue.AuthorId);
    }

    [Fact]
    public async Task Create_DoesNotRequireAuthorization()
    {
        await SeedProject();

        await _service.Create(ProjectId, new CreateIssueRequest { Title = "X" });

        _authGuard.Verify(g => g.Authorize(It.IsAny<object>(), It.IsAny<IAuthorizationRequirement>()), Times.Never);
    }

    [Fact]
    public async Task Update_UpdatesFields_WhenMember()
    {
        await SeedProject();
        var issue = new Issue { Id = IssueId, ProjectId = ProjectId, Title = "Issue", AuthorId = 99, Status = Status.Open };
        _db.Issues.Add(issue);
        await _db.SaveChangesAsync();

        var result = await _service.Update(ProjectId, IssueId, new UpdateIssueRequest
        {
            Title = "Updated",
            Description = "New"
        });

        Assert.Equal("Updated", result.Title);
        Assert.Equal("New", result.Description);
    }

    [Fact]
    public async Task Update_Throws_WhenNotProjectMember()
    {
        await SeedProject();
        var issue = new Issue { Id = IssueId, ProjectId = ProjectId, Title = "Issue", AuthorId = 99, Status = Status.Open };
        _db.Issues.Add(issue);
        await _db.SaveChangesAsync();

        _authGuard.Setup(g => g.Authorize(It.IsAny<Issue>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not member"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Update(ProjectId, IssueId, new UpdateIssueRequest { Title = "X" }));
    }

    [Fact]
    public async Task Update_Throws_WhenProjectNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Update(ProjectId, IssueId, new UpdateIssueRequest { Title = "X" }));
    }

    [Fact]
    public async Task Delete_DeletesIssue_WhenOwnerOrLead()
    {
        await SeedProject();
        var issue = new Issue { Id = IssueId, ProjectId = ProjectId, Title = "Issue", AuthorId = CurrentUserId, Status = Status.Open };
        _db.Issues.Add(issue);
        await _db.SaveChangesAsync();

        await _service.Delete(ProjectId, IssueId);

        _attachmentsService.Verify(s => s.DeleteAll(IssueId), Times.Once);
        Assert.Null(await _db.Issues.FindAsync(IssueId));
    }

    [Fact]
    public async Task Delete_Throws_WhenNotOwnerOrLead()
    {
        await SeedProject();
        var issue = new Issue { Id = IssueId, ProjectId = ProjectId, Title = "Issue", AuthorId = 99, Status = Status.Open };
        _db.Issues.Add(issue);
        await _db.SaveChangesAsync();

        _authGuard.Setup(g => g.Authorize(It.IsAny<Issue>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not owner or lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Delete(ProjectId, IssueId));

        Assert.NotNull(await _db.Issues.FindAsync(IssueId));
    }

    [Fact]
    public async Task GetById_ReturnsIssueDto()
    {
        await SeedProject();
        var issue = new Issue { Id = IssueId, ProjectId = ProjectId, Title = "Issue", AuthorId = 99, Status = Status.Open };
        _db.Issues.Add(issue);
        await _db.SaveChangesAsync();

        var result = await _service.GetById(ProjectId, IssueId);

        Assert.Equal(IssueId, result.Id);
        Assert.Equal("Issue", result.Title);
    }

    [Fact]
    public async Task GetById_Throws_WhenProjectNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetById(ProjectId, IssueId));
    }

    [Fact]
    public async Task GetAll_ReturnsIssues_WhenProjectExists()
    {
        await SeedProject();

        var result = await _service.GetAll(ProjectId, new IssueQuery(null, null, null, null));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCurrentUserIssues_ReturnsEmpty_WhenNoIssues()
    {
        var result = await _service.GetCurrentUserIssues(new IssueQuery(null, null, null, null));

        Assert.Empty(result);
    }
}
