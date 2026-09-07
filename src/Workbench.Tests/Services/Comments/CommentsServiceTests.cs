using Microsoft.Extensions.Logging;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Auth.Models;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Requirements;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Comments.Dtos;
using Workbench.Modules.Comments.Dtos.Requests;
using Workbench.Modules.Comments.Models;
using Workbench.Modules.Comments.Repositories;
using Workbench.Modules.Comments.Services.Implementations;
using Workbench.Modules.Issues.Repositories;

namespace Workbench.Tests.Services.Comments;

public class CommentsServiceTests
{
    private const int CurrentUserId = 123;
    private const string CurrentUsername = "test";
    private const int DefaultIssueId = 1;

    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly Mock<ICommentsRepository> _commentsRepo;
    private readonly Mock<IIssuesRepository> _issuesRepo;
    private readonly CommentsService _service;

    public CommentsServiceTests()
    {
        var userMock = new Mock<ICurrentUser>();
        userMock.Setup(u => u.Id).Returns(CurrentUserId);
        userMock.Setup(u => u.UserName).Returns(CurrentUsername);

        _authGuard = new Mock<IAuthorizationGuard>();
        _commentsRepo = new Mock<ICommentsRepository>();
        _issuesRepo = new Mock<IIssuesRepository>();

        _service = new CommentsService(
            userMock.Object,
            Mock.Of<ILogger<CommentsService>>(),
            _authGuard.Object,
            _commentsRepo.Object,
            _issuesRepo.Object);
    }

    private static Comment MakeComment(int id, int authorId = CurrentUserId,
        string content = "content") =>
        new()
        {
            Id = id,
            IssueId = DefaultIssueId,
            AuthorId = authorId,
            Content = content,
            Author = new ApplicationUser { Id = authorId, UserName = "user" }
        };

    [Fact]
    public async Task GetAll_ReturnsCommentsFromRepository()
    {
        var expected = new List<CommentDto>
        {
            new(1, "First", DateTime.UtcNow, CurrentUsername, []),
            new(2, "Second", DateTime.UtcNow, CurrentUsername, [])
        };

        _commentsRepo
            .Setup(r => r.GetAllByIssueIdAsync(DefaultIssueId))
            .ReturnsAsync(expected);

        var result = await _service.GetAll(DefaultIssueId);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Create_SavesCommentWithCorrectFields()
    {
        Comment? captured = null;
        _commentsRepo
            .Setup(r => r.Add(It.IsAny<Comment>()))
            .Callback<Comment>(c => captured = c)
            .Returns((Comment c) => c);

        var result = await _service.Create(DefaultIssueId, new CreateCommentRequest("hello"));

        Assert.NotNull(captured);
        Assert.Equal(DefaultIssueId, captured.IssueId);
        Assert.Equal(CurrentUserId, captured.AuthorId);
        Assert.Equal("hello", captured.Content);

        Assert.Equal("hello", result.Content);
        Assert.Equal(CurrentUsername, result.AuthorUsername);

    }

    [Fact]
    public async Task Create_DoesNotSave_WhenIssueDoesNotExist()
    {
        _issuesRepo
            .Setup(r => r.ExistsOrThrowAsync(999))
            .ThrowsAsync(new NotFoundException("Issue 999 not found"));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Create(999, new CreateCommentRequest("content")));

        _commentsRepo.Verify(r => r.Add(It.IsAny<Comment>()), Times.Never);
    }

    [Fact]
    public async Task Update_SavesNewContent()
    {
        var comment = MakeComment(1, content: "original");
        _commentsRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(comment);

        var result = await _service.Update(1, new UpdateCommentRequest("updated"));

        Assert.Equal("updated", comment.Content);
        Assert.Equal("updated", result.Content);
    }

    [Fact]
    public async Task Update_DoesNotSave_WhenUnauthorized()
    {
        var comment = MakeComment(1, 999, "protected");
        _commentsRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(comment);

        _authGuard
            .Setup(g => g.Authorize(comment, It.IsAny<OwnerOrTeamMemberRequirement>()))
            .ThrowsAsync(new UnauthorizedAccessException("Not authorized"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.Update(1, new UpdateCommentRequest("hijacked")));

        Assert.Equal("protected", comment.Content);
    }

    [Fact]
    public async Task Delete_RemovesAndSaves()
    {
        var comment = MakeComment(1, content: "bye");
        _commentsRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(comment);

        await _service.Delete(1);

        _commentsRepo.Verify(r => r.Remove(comment), Times.Once);
    }

    [Fact]
    public async Task Delete_DoesNotRemove_WhenUnauthorized()
    {
        var comment = MakeComment(1, 999, "protected");
        _commentsRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(comment);

        _authGuard
            .Setup(g => g.Authorize(comment, It.IsAny<OwnerOrTeamMemberRequirement>()))
            .ThrowsAsync(new UnauthorizedAccessException("Not authorized"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.Delete(1));

        _commentsRepo.Verify(r => r.Remove(It.IsAny<Comment>()), Times.Never);
    }
}
