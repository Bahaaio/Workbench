using Microsoft.Extensions.Logging;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Auth.Models;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Requirements;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Comments.Dtos.Requests;
using Workbench.Modules.Comments.Services.Implementations;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Issues.Models;
using Workbench.Tests.Helpers;

namespace Workbench.Tests.Services.Comments;

public class CommentsServiceTests : IDisposable
{
    private const int CurrentUserId = 123;
    private const string CurrentUsername = "test";
    private const int DefaultIssueId = 1;

    private readonly Data.AppDbContext _db;
    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly CommentsService _service;

    public CommentsServiceTests()
    {
        _db = TestDbContextFactory.Create();

        var userMock = new Mock<ICurrentUser>();
        userMock.Setup(u => u.Id).Returns(CurrentUserId);
        userMock.Setup(u => u.UserName).Returns(CurrentUsername);

        _authGuard = new Mock<IAuthorizationGuard>();

        _service = new CommentsService(
            _db,
            userMock.Object,
            Mock.Of<ILogger<CommentsService>>(),
            _authGuard.Object);
    }

    public void Dispose() => _db.Dispose();

    private async Task SeedIssue()
    {
        var author = new ApplicationUser { Id = 99, UserName = "author" };
        _db.Users.Add(author);
        var owner = new ApplicationUser { Id = 1, UserName = "owner" };
        _db.Users.Add(owner);
        _db.Projects.Add(new Modules.Projects.Models.Project
        {
            Id = 1,
            OwnerId = 1,
            Name = "P",
            Description = null,
        });
        _db.Issues.Add(new Issue
        {
            Id = DefaultIssueId,
            ProjectId = 1,
            Title = "Issue",
            AuthorId = 99,
            Status = Status.Open,
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAll_ReturnsCommentsFromDb()
    {
        await SeedIssue();
        var user = new ApplicationUser { Id = CurrentUserId, UserName = CurrentUsername };
        _db.Users.Add(user);
        _db.Comments.Add(new Modules.Comments.Models.Comment
        {
            Id = 1,
            IssueId = DefaultIssueId,
            AuthorId = CurrentUserId,
            Content = "First",
        });
        _db.Comments.Add(new Modules.Comments.Models.Comment
        {
            Id = 2,
            IssueId = DefaultIssueId,
            AuthorId = CurrentUserId,
            Content = "Second",
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetAll(DefaultIssueId);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Create_SavesCommentWithCorrectFields()
    {
        await SeedIssue();
        var user = new ApplicationUser { Id = CurrentUserId, UserName = CurrentUsername };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _service.Create(DefaultIssueId, new CreateCommentRequest("hello"));

        Assert.Equal("hello", result.Content);
        Assert.Equal(CurrentUsername, result.AuthorUsername);
        var comment = _db.Comments.Single(c => c.IssueId == DefaultIssueId);
        Assert.Equal(CurrentUserId, comment.AuthorId);
    }

    [Fact]
    public async Task Create_DoesNotSave_WhenIssueDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Create(999, new CreateCommentRequest("content")));
    }

    [Fact]
    public async Task Update_SavesNewContent()
    {
        await SeedIssue();
        var user = new ApplicationUser { Id = CurrentUserId, UserName = CurrentUsername };
        _db.Users.Add(user);
        _db.Comments.Add(new Modules.Comments.Models.Comment
        {
            Id = 1,
            IssueId = DefaultIssueId,
            AuthorId = CurrentUserId,
            Content = "original",
        });
        await _db.SaveChangesAsync();

        var result = await _service.Update(1, new UpdateCommentRequest("updated"));

        Assert.Equal("updated", result.Content);
        var comment = await _db.Comments.FindAsync(1);
        Assert.Equal("updated", comment!.Content);
    }

    [Fact]
    public async Task Update_DoesNotSave_WhenUnauthorized()
    {
        await SeedIssue();
        var otherUser = new ApplicationUser { Id = 999, UserName = "other" };
        _db.Users.Add(otherUser);
        _db.Comments.Add(new Modules.Comments.Models.Comment
        {
            Id = 1,
            IssueId = DefaultIssueId,
            AuthorId = 999,
            Content = "protected",
        });
        await _db.SaveChangesAsync();

        _authGuard
            .Setup(g => g.Authorize(It.IsAny<Modules.Comments.Models.Comment>(), It.IsAny<OwnerOrTeamMemberRequirement>()))
            .ThrowsAsync(new UnauthorizedAccessException("Not authorized"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.Update(1, new UpdateCommentRequest("hijacked")));

        var comment = await _db.Comments.FindAsync(1);
        Assert.Equal("protected", comment!.Content);
    }

    [Fact]
    public async Task Delete_RemovesAndSaves()
    {
        await SeedIssue();
        var user = new ApplicationUser { Id = CurrentUserId, UserName = CurrentUsername };
        _db.Users.Add(user);
        _db.Comments.Add(new Modules.Comments.Models.Comment
        {
            Id = 1,
            IssueId = DefaultIssueId,
            AuthorId = CurrentUserId,
            Content = "bye",
        });
        await _db.SaveChangesAsync();

        await _service.Delete(1);

        Assert.Null(await _db.Comments.FindAsync(1));
    }

    [Fact]
    public async Task Delete_DoesNotRemove_WhenUnauthorized()
    {
        await SeedIssue();
        var otherUser = new ApplicationUser { Id = 999, UserName = "other" };
        _db.Users.Add(otherUser);
        _db.Comments.Add(new Modules.Comments.Models.Comment
        {
            Id = 1,
            IssueId = DefaultIssueId,
            AuthorId = 999,
            Content = "protected",
        });
        await _db.SaveChangesAsync();

        _authGuard
            .Setup(g => g.Authorize(It.IsAny<Modules.Comments.Models.Comment>(), It.IsAny<OwnerOrTeamMemberRequirement>()))
            .ThrowsAsync(new UnauthorizedAccessException("Not authorized"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.Delete(1));

        Assert.NotNull(await _db.Comments.FindAsync(1));
    }
}
