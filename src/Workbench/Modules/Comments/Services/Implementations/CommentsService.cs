using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Comments.Dtos;
using Workbench.Modules.Comments.Dtos.Requests;
using Workbench.Modules.Comments.Mappers;
using Workbench.Modules.Comments.Models;
using Workbench.Modules.Comments.Repositories;
using Workbench.Modules.Issues.Repositories;

namespace Workbench.Modules.Comments.Services.Implementations;

public class CommentsService : ICommentsService
{
    private readonly IAuthorizationGuard _authGuard;
    private readonly ICommentsRepository _commentsRepository;
    private readonly IIssuesRepository _issuesRepository;
    private readonly ILogger<CommentsService> _logger;
    private readonly ICurrentUser _user;

    public CommentsService(ICurrentUser user, ILogger<CommentsService> logger,
        IAuthorizationGuard authGuard, ICommentsRepository commentsRepository,
        IIssuesRepository issuesRepository)
    {
        _authGuard = authGuard;
        _commentsRepository = commentsRepository;
        _issuesRepository = issuesRepository;
        _user = user;
        _logger = logger;
    }

    public Task<List<CommentDto>> GetAll(int issueId) =>
        _commentsRepository.GetAllByIssueIdAsync(issueId);

    public async Task<CommentDto> Create(int issueId, CreateCommentRequest request)
    {
        await _issuesRepository.ExistsOrThrowAsync(issueId);

        var comment = new Comment
        {
            Content = request.Content,
            IssueId = issueId,
            AuthorId = _user.Id
        };

        _commentsRepository.Add(comment);
        await _commentsRepository.SaveChangesAsync();

        _logger.LogInformation("User {userId} created comment {commentId} on issue {issueId}",
            _user.Id, comment.Id, issueId);

        // Author is always the current user, build the DTO directly without an extra DB round trip.
        return new CommentDto(comment.Id, comment.Content, comment.CreatedAt, _user.UserName, []);
    }

    public async Task<CommentDto> Update(int commentId, UpdateCommentRequest request)
    {
        var comment = await _commentsRepository.GetByIdAsync(commentId);
        await _authGuard.AuthorizeOwnerOrProjectMember(comment);

        comment.Content = request.Content;
        await _commentsRepository.SaveChangesAsync();

        _logger.LogInformation("User {userId} updated comment {commentId}", _user.Id, commentId);

        return comment.ToDto();
    }

    public async Task Delete(int commentId)
    {
        var comment = await _commentsRepository.GetByIdAsync(commentId);
        await _authGuard.AuthorizeOwnerOrProjectMember(comment);

        _commentsRepository.Remove(comment);
        await _commentsRepository.SaveChangesAsync();

        _logger.LogInformation("User {userId} deleted comment {commentId}", _user.Id, commentId);
    }
}