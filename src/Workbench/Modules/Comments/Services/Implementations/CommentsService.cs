using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Comments.Authorization;
using Workbench.Modules.Comments.Dtos;
using Workbench.Modules.Comments.Dtos.Requests;
using Workbench.Modules.Comments.Mappers;
using Workbench.Modules.Comments.Models;
using Workbench.Modules.Issues.Models;

namespace Workbench.Modules.Comments.Services.Implementations;

public class CommentsService : ICommentsService
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationGuard _authGuard;
    private readonly ILogger<CommentsService> _logger;
    private readonly ICurrentUser _user;

    public CommentsService(AppDbContext dbContext, ICurrentUser user, ILogger<CommentsService> logger,
        IAuthorizationGuard authGuard)
    {
        _db = dbContext;
        _authGuard = authGuard;
        _user = user;
        _logger = logger;
    }

    public async Task<List<CommentDto>> GetAll(int issueId)
    {
        await _db.Issues.ExistsOrThrowAsync(issueId);

        return await _db.Comments
            .AsNoTracking()
            .Where(c => c.IssueId == issueId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(CommentMapper.ToDtoExpression)
            .ToListAsync();
    }

    public async Task<CommentDto> Create(int issueId, CreateCommentRequest request)
    {
        await _db.Issues.ExistsOrThrowAsync(issueId);

        var comment = new Comment
        {
            Content = request.Content,
            IssueId = issueId,
            AuthorId = _user.Id
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {userId} created comment {commentId} on issue {issueId}",
            _user.Id, comment.Id, issueId);

        return new CommentDto(comment.Id, comment.Content, comment.CreatedAt, _user.UserName, []);
    }

    public async Task<CommentDto> Update(int commentId, UpdateCommentRequest request)
    {
        var comment = await _db.Comments
                          .Where(c => c.Id == commentId)
                          .Include(c => c.Author)
                          .Include(c => c.Attachments)
                          .Include(c => c.Issue).ThenInclude(i => i.Project)
                          .SingleOrDefaultAsync()
                      ?? throw new NotFoundException($"Comment with id: {commentId} not found");

        await _authGuard.Authorize(comment, new CommentEditRequirement());

        comment.Content = request.Content;
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {userId} updated comment {commentId}", _user.Id, commentId);

        return comment.ToDto();
    }

    public async Task Delete(int commentId)
    {
        var comment = await _db.Comments
            .Where(c => c.Id == commentId)
            .Include(c => c.Author)
            .Include(c => c.Attachments)
            .Include(c => c.Issue).ThenInclude(i => i.Project)
            .SingleOrDefaultAsync()
            ?? throw new NotFoundException($"Comment with id: {commentId} not found");

        await _authGuard.AuthorizeOwnerOrProjectMember(comment);

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {userId} deleted comment {commentId}", _user.Id, commentId);
    }
}
