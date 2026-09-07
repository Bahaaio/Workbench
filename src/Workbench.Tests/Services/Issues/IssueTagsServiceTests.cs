using Microsoft.AspNetCore.Authorization;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Common.Enums;
using Workbench.Modules.Auth.Models;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Issues.Services.Implementations;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Models;
using Workbench.Modules.Tags.Models;
using Workbench.Tests.Helpers;

namespace Workbench.Tests.Services.Issues;

public class IssueTagsServiceTests : IDisposable
{
    private const int ProjectId = 1;
    private const int IssueId = 100;

    private readonly Data.AppDbContext _db;
    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly IssueTagsService _service;

    public IssueTagsServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _authGuard = new Mock<IAuthorizationGuard>();
        _service = new IssueTagsService(_db, _authGuard.Object);
    }

    public void Dispose() => _db.Dispose();

    private async Task SeedProject(int projectId = ProjectId)
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
        await _db.SaveChangesAsync();
    }

    private async Task SeedIssue(List<Tag>? tags = null, bool createProject = true)
    {
        var author = new ApplicationUser { Id = 99, UserName = "author" };
        _db.Users.Add(author);
        if (createProject)
        {
            var owner = new ApplicationUser { Id = 1, UserName = "owner" };
            _db.Users.Add(owner);
            _db.Projects.Add(new Project { Id = ProjectId, OwnerId = 1, Name = "P", Description = null, Visibility = ProjectVisibility.Public });
        }
        var issue = new Issue
        {
            Id = IssueId,
            ProjectId = ProjectId,
            Title = "Issue",
            AuthorId = 99,
            Tags = tags ?? [],
        };
        _db.Issues.Add(issue);
        await _db.SaveChangesAsync();
    }

    private async Task<Tag> SeedTag(string name, int projectId = ProjectId)
    {
        var tag = new Tag
        {
            ProjectId = projectId,
            Name = name,
            Description = null,
            Color = Color.Blue,
        };
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();
        return tag;
    }

    [Fact]
    public async Task UpdateTags_ReplacesTags_WhenAllExist()
    {
        await SeedIssue();
        await SeedTag("bug");
        await SeedTag("urgent");

        var result = await _service.UpdateTags(IssueId, ["bug", "urgent"]);

        Assert.Equal(2, result.Count);
        Assert.Contains("bug", result);
        Assert.Contains("urgent", result);
    }

    [Fact]
    public async Task UpdateTags_LowercasesTagNames()
    {
        await SeedIssue();
        await SeedTag("bug");

        var result = await _service.UpdateTags(IssueId, ["BUG"]);

        Assert.Contains("bug", result);
    }

    [Fact]
    public async Task UpdateTags_Throws_WhenIssueNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateTags(IssueId, ["tag"]));
    }

    [Fact]
    public async Task UpdateTags_Throws_WhenTagsNotFound()
    {
        await SeedIssue();

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateTags(IssueId, ["nonexistent"]));
    }

    [Fact]
    public async Task UpdateTags_Throws_WhenSomeTagsNotFound()
    {
        await SeedIssue();
        await SeedTag("bug");

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateTags(IssueId, ["bug", "missing"]));

        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public async Task UpdateTags_ClearsExistingTags()
    {
        await SeedProject();
        var existingTag = await SeedTag("old");
        await SeedIssue(tags: [existingTag], createProject: false);
        await SeedTag("new");

        var result = await _service.UpdateTags(IssueId, ["new"]);

        Assert.Single(result);
        Assert.Contains("new", result);
        Assert.DoesNotContain("old", result);
    }

    [Fact]
    public async Task UpdateTags_Throws_WhenNotProjectMember()
    {
        await SeedIssue();
        _authGuard.Setup(g => g.Authorize(It.IsAny<Issue>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not member"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.UpdateTags(IssueId, ["tag"]));
    }

    [Fact]
    public async Task UpdateTags_HandlesEmptyTagList()
    {
        await SeedProject();
        var existingTag = await SeedTag("existing");
        await SeedIssue(tags: [existingTag], createProject: false);

        var result = await _service.UpdateTags(IssueId, []);

        Assert.Empty(result);
        var issue = await _db.Issues.FindAsync(IssueId);
        Assert.Empty(issue!.Tags);
    }
}
