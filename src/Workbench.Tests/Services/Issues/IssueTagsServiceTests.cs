using Microsoft.AspNetCore.Authorization;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Common.Enums;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Issues.Repositories;
using Workbench.Modules.Issues.Services.Implementations;
using Workbench.Modules.Tags.Models;
using Workbench.Modules.Tags.Repositories;

namespace Workbench.Tests.Services.Issues;

public class IssueTagsServiceTests
{
    private const int ProjectId = 1;
    private const int IssueId = 100;

    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly Mock<IIssuesRepository> _issuesRepo;
    private readonly Mock<ITagsRepository> _tagsRepo;
    private readonly IssueTagsService _service;

    public IssueTagsServiceTests()
    {
        _authGuard = new Mock<IAuthorizationGuard>();
        _issuesRepo = new Mock<IIssuesRepository>();
        _tagsRepo = new Mock<ITagsRepository>();

        _service = new IssueTagsService(
            _issuesRepo.Object,
            _tagsRepo.Object,
            _authGuard.Object);
    }

    private static Issue MakeIssue(List<Tag>? tags = null) =>
        new()
        {
            Id = IssueId,
            ProjectId = ProjectId,
            Title = "Issue",
            AuthorId = 99,
            Tags = tags ?? [],
            Author = new Modules.Auth.Models.ApplicationUser { Id = 99, UserName = "author" }
        };

    private static Tag MakeTag(string name, int projectId = ProjectId) =>
        new()
        {
            Id = 1,
            ProjectId = projectId,
            Name = name,
            Description = null,
            Color = Color.Blue
        };

    [Fact]
    public async Task UpdateTags_ReplacesTags_WhenAllExist()
    {
        var issue = MakeIssue();
        _issuesRepo.Setup(r => r.FindWithTagsAsync(IssueId)).ReturnsAsync(issue);

        var tag1 = MakeTag("bug");
        var tag2 = MakeTag("urgent");
        _tagsRepo.Setup(r => r.GetByNamesAsync(ProjectId, It.IsAny<List<string>>()))
            .ReturnsAsync(new List<Tag> { tag1, tag2 });

        var result = await _service.UpdateTags(IssueId, ["bug", "urgent"]);

        Assert.Equal(2, result.Count);
        Assert.Contains("bug", result);
        Assert.Contains("urgent", result);
    }

    [Fact]
    public async Task UpdateTags_LowercasesTagNames()
    {
        var issue = MakeIssue();
        _issuesRepo.Setup(r => r.FindWithTagsAsync(IssueId)).ReturnsAsync(issue);

        var tag = MakeTag("bug");
        _tagsRepo.Setup(r => r.GetByNamesAsync(ProjectId, It.IsAny<List<string>>()))
            .ReturnsAsync(new List<Tag> { tag });

        var result = await _service.UpdateTags(IssueId, ["BUG"]);

        Assert.Contains("bug", result);
        _tagsRepo.Verify(r => r.GetByNamesAsync(ProjectId, It.Is<List<string>>(names =>
            names.Contains("bug"))), Times.Once);
    }

    [Fact]
    public async Task UpdateTags_Throws_WhenIssueNotFound()
    {
        _issuesRepo.Setup(r => r.FindWithTagsAsync(IssueId))
            .ThrowsAsync(new NotFoundException("Not found"));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateTags(IssueId, ["tag"]));

    }

    [Fact]
    public async Task UpdateTags_Throws_WhenTagsNotFound()
    {
        var issue = MakeIssue();
        _issuesRepo.Setup(r => r.FindWithTagsAsync(IssueId)).ReturnsAsync(issue);
        _tagsRepo.Setup(r => r.GetByNamesAsync(ProjectId, It.IsAny<List<string>>()))
            .ReturnsAsync(new List<Tag>());

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateTags(IssueId, ["nonexistent"]));

    }

    [Fact]
    public async Task UpdateTags_Throws_WhenSomeTagsNotFound()
    {
        var issue = MakeIssue();
        _issuesRepo.Setup(r => r.FindWithTagsAsync(IssueId)).ReturnsAsync(issue);

        var tag1 = MakeTag("bug");
        _tagsRepo.Setup(r => r.GetByNamesAsync(ProjectId, It.IsAny<List<string>>()))
            .ReturnsAsync(new List<Tag> { tag1 });

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateTags(IssueId, ["bug", "missing"]));

        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public async Task UpdateTags_ClearsExistingTags()
    {
        var existingTag = MakeTag("old");
        var issue = MakeIssue(tags: [existingTag]);
        _issuesRepo.Setup(r => r.FindWithTagsAsync(IssueId)).ReturnsAsync(issue);

        var newTag = MakeTag("new");
        _tagsRepo.Setup(r => r.GetByNamesAsync(ProjectId, It.IsAny<List<string>>()))
            .ReturnsAsync(new List<Tag> { newTag });

        var result = await _service.UpdateTags(IssueId, ["new"]);

        Assert.Single(result);
        Assert.Contains("new", result);
        Assert.DoesNotContain("old", result);
    }

    [Fact]
    public async Task UpdateTags_Throws_WhenNotProjectMember()
    {
        var issue = MakeIssue();
        _issuesRepo.Setup(r => r.FindWithTagsAsync(IssueId)).ReturnsAsync(issue);
        _authGuard.Setup(g => g.Authorize(It.IsAny<Issue>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not member"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.UpdateTags(IssueId, ["tag"]));
    }

    [Fact]
    public async Task UpdateTags_HandlesEmptyTagList()
    {
        var existingTag = MakeTag("existing");
        var issue = MakeIssue(tags: [existingTag]);
        _issuesRepo.Setup(r => r.FindWithTagsAsync(IssueId)).ReturnsAsync(issue);

        _tagsRepo.Setup(r => r.GetByNamesAsync(ProjectId, It.IsAny<List<string>>()))
            .ReturnsAsync(new List<Tag>());

        var result = await _service.UpdateTags(IssueId, []);

        Assert.Empty(result);
        Assert.Empty(issue.Tags);
    }
}
