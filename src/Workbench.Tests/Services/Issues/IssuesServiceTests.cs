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
using Workbench.Modules.Issues.Repositories;
using Workbench.Modules.Issues.Services.Implementations;
using Workbench.Modules.Projects.Models;
using Workbench.Modules.Projects.Repositories;

namespace Workbench.Tests.Services.Issues;

public class IssuesServiceTests
{
    private const int CurrentUserId = 10;
    private const int ProjectId = 1;
    private const int IssueId = 100;

    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly Mock<IIssuesRepository> _issuesRepo;
    private readonly Mock<IProjectsRepository> _projectsRepo;
    private readonly Mock<IAttachmentsService<Issue>> _attachmentsService;
    private readonly IssuesService _service;

    public IssuesServiceTests()
    {
        var userMock = new Mock<ICurrentUser>();
        userMock.Setup(u => u.Id).Returns(CurrentUserId);

        _authGuard = new Mock<IAuthorizationGuard>();
        _issuesRepo = new Mock<IIssuesRepository>();
        _projectsRepo = new Mock<IProjectsRepository>();
        _attachmentsService = new Mock<IAttachmentsService<Issue>>();

        _service = new IssuesService(
            _issuesRepo.Object,
            userMock.Object,
            _authGuard.Object,
            Mock.Of<ILogger<IssuesService>>(),
            _attachmentsService.Object,
            _projectsRepo.Object);
    }

    private static Issue MakeIssue(int projectId = ProjectId, int authorId = 99) =>
        new()
        {
            Id = IssueId,
            ProjectId = projectId,
            Title = "Issue",
            AuthorId = authorId,
            Author = new Modules.Auth.Models.ApplicationUser { Id = authorId, UserName = "author" },
            Project = new Project
            {
                Id = projectId,
                OwnerId = 1,
                Name = "P",
                Description = null,
                Owner = new Modules.Auth.Models.ApplicationUser { Id = 1, UserName = "owner" }
            },
            Tags = [],
            Attachments = [],
            Votes = []
        };

    private static Project MakeProject() =>
        new()
        {
            Id = ProjectId,
            OwnerId = 1,
            Name = "P",
            Description = null,
            Owner = new Modules.Auth.Models.ApplicationUser { Id = 1, UserName = "owner" }
        };

    [Fact]
    public async Task Create_CreatesIssueWithAuthorId()
    {
        var project = MakeProject();
        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);
        _issuesRepo.Setup(r => r.Add(It.IsAny<Issue>()))
            .Callback<Issue>(i => i.Id = IssueId);
        _issuesRepo.Setup(r => r.LoadAuthorAsync(It.IsAny<Issue>()))
            .Callback<Issue>(i => i.Author = new Modules.Auth.Models.ApplicationUser { Id = CurrentUserId, UserName = "current" })
            .Returns(Task.CompletedTask);

        var result = await _service.Create(ProjectId, new CreateIssueRequest
        {
            Title = "New Issue",
            Description = "Desc"
        });

        Assert.Equal(IssueId, result.Id);
        Assert.Equal("New Issue", result.Title);
        _issuesRepo.Verify(r => r.Add(It.Is<Issue>(i => i.AuthorId == CurrentUserId)), Times.Once);
    }

    [Fact]
    public async Task Create_DoesNotRequireAuthorization()
    {
        var project = MakeProject();
        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);
        _issuesRepo.Setup(r => r.Add(It.IsAny<Issue>()))
            .Callback<Issue>(i => i.Id = IssueId);
        _issuesRepo.Setup(r => r.LoadAuthorAsync(It.IsAny<Issue>()))
            .Callback<Issue>(i => i.Author = new Modules.Auth.Models.ApplicationUser { Id = CurrentUserId, UserName = "current" })
            .Returns(Task.CompletedTask);

        await _service.Create(ProjectId, new CreateIssueRequest { Title = "X" });

        _authGuard.Verify(g => g.Authorize(It.IsAny<object>(), It.IsAny<IAuthorizationRequirement>()), Times.Never);
    }

    [Fact]
    public async Task Update_UpdatesFields_WhenMember()
    {
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        var issue = MakeIssue();
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

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
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        var issue = MakeIssue();
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);
        _authGuard.Setup(g => g.Authorize(It.IsAny<Issue>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not member"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Update(ProjectId, IssueId, new UpdateIssueRequest { Title = "X" }));

    }

    [Fact]
    public async Task Update_Throws_WhenProjectNotFound()
    {
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId))
            .ThrowsAsync(new NotFoundException("Not found"));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Update(ProjectId, IssueId, new UpdateIssueRequest { Title = "X" }));
    }

    [Fact]
    public async Task Delete_DeletesIssue_WhenOwnerOrLead()
    {
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        var issue = MakeIssue(authorId: CurrentUserId);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        await _service.Delete(ProjectId, IssueId);

        _attachmentsService.Verify(s => s.DeleteAll(IssueId), Times.Once);
        _issuesRepo.Verify(r => r.Remove(issue), Times.Once);
    }

    [Fact]
    public async Task Delete_Throws_WhenNotOwnerOrLead()
    {
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        var issue = MakeIssue(authorId: 99);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);
        _authGuard.Setup(g => g.Authorize(It.IsAny<Issue>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not owner or lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Delete(ProjectId, IssueId));

        _issuesRepo.Verify(r => r.Remove(It.IsAny<Issue>()), Times.Never);
    }

    [Fact]
    public async Task Delete_DeletesAttachmentsBeforeRemoving()
    {
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        var issue = MakeIssue(authorId: CurrentUserId);
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        var callOrder = new List<string>();
        _attachmentsService.Setup(s => s.DeleteAll(IssueId))
            .Callback(() => callOrder.Add("attachments"));
        _issuesRepo.Setup(r => r.Remove(issue))
            .Callback(() => callOrder.Add("issue"));

        await _service.Delete(ProjectId, IssueId);

        Assert.Equal("attachments", callOrder[0]);
        Assert.Equal("issue", callOrder[1]);
    }

    [Fact]
    public async Task GetById_ReturnsIssueDto()
    {
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        var issue = MakeIssue();
        _issuesRepo.Setup(r => r.GetByIdAsync(IssueId)).ReturnsAsync(issue);

        var result = await _service.GetById(ProjectId, IssueId);

        Assert.Equal(IssueId, result.Id);
        Assert.Equal("Issue", result.Title);
    }

    [Fact]
    public async Task GetById_Throws_WhenProjectNotFound()
    {
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId))
            .ThrowsAsync(new NotFoundException("Not found"));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetById(ProjectId, IssueId));
    }

    [Fact]
    public async Task GetAll_ReturnsIssues_WhenProjectExists()
    {
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _issuesRepo.Setup(r => r.GetAllAsync(ProjectId, It.IsAny<IssueQuery>()))
            .ReturnsAsync(new List<Modules.Issues.Dtos.IssueDto>());

        var result = await _service.GetAll(ProjectId, new IssueQuery(null, null, null, null));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCurrentUserIssues_DelegatesToRepository()
    {
        _issuesRepo.Setup(r => r.GetAllByAuthorAsync(CurrentUserId, It.IsAny<IssueQuery>()))
            .ReturnsAsync(new List<Modules.Issues.Dtos.IssueDto>());

        var result = await _service.GetCurrentUserIssues(new IssueQuery(null, null, null, null));

        Assert.Empty(result);
    }
}
