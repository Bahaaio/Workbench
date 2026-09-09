using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Workbench.Common.Exceptions;
using Workbench.Data;
using Workbench.Modules.Attachments.Dtos;
using Workbench.Modules.Attachments.Options;
using Workbench.Modules.Attachments.Services;
using Workbench.Modules.Attachments.Services.Implementations;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Comments.Models;
using Workbench.Modules.Comments.Options;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Storage.Services;

namespace Workbench.Modules.Comments.Services.Implementations;

public class CommentAttachmentsService : AttachmentsService<Comment, CommentAttachment>
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationGuard _authGuard;
    private readonly ICurrentUser _user;
    private readonly CommentAttachmentOptions _options;

    public CommentAttachmentsService(
        IStorageService storageService,
        AppDbContext dbContext,
        ICurrentUser user,
        ILogger<AttachmentsService<Comment, CommentAttachment>> logger,
        IAttachmentValidationService attachmentValidationService,
        IOptions<CommentAttachmentOptions> attachmentOptions,
        IAuthorizationGuard authGuard)
        : base(storageService, dbContext, user, logger,
            attachmentValidationService)
    {
        _db = dbContext;
        _authGuard = authGuard;
        _user = user;
        _options = attachmentOptions.Value;
    }

    protected override AttachmentOptions AttachmentOptions => _options;

    protected override AttachmentOptions GetResolvedOptions(Comment parent)
    {
        var isLead = _db.ProjectMemberships
            .Any(m => m.ProjectId == parent.Issue.ProjectId && m.UserId == _user.Id
                && m.Role == ProjectMemberRole.Lead);

        return new CommentAttachmentOptions
        {
            MaxSizeBytes = isLead ? _options.MaxSizeBytesLead : _options.MaxSizeBytes,
            MaxCount = _options.MaxCount,
            AllowedExtensions = _options.AllowedExtensions,
            MaxSizeBytesLead = _options.MaxSizeBytesLead
        };
    }

    public override async Task<AttachmentDto> Add(int parentId, IFormFile file)
    {
        var comment = await GetOwnerEntity(parentId);
        await _authGuard.AuthorizeOwnerOrProjectMember(comment);

        return await base.Add(parentId, file);
    }

    public override async Task Delete(Guid attachmentId)
    {
        var comment = await GetOwnerEntity(attachmentId);
        await _authGuard.AuthorizeOwnerOrProjectMember(comment);

        await base.Delete(attachmentId);
    }

    protected override async Task<Comment> GetOwnerEntity(int parentId) =>
        await _db.Comments
            .Include(c => c.Issue)
            .SingleOrDefaultAsync(c => c.Id == parentId)
        ?? throw new NotFoundException($"Comment with id {parentId} not found");
}
