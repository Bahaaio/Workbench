using Microsoft.Extensions.Options;
using Workbench.Data;
using Workbench.Modules.Attachments.Dtos;
using Workbench.Modules.Attachments.Options;
using Workbench.Modules.Attachments.Repositories;
using Workbench.Modules.Attachments.Services;
using Workbench.Modules.Attachments.Services.Implementations;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Comments.Models;
using Workbench.Modules.Comments.Options;
using Workbench.Modules.Storage.Services;

namespace Workbench.Modules.Comments.Services.Implementations;

public class CommentAttachmentsService : AttachmentsService<Comment, CommentAttachment>
{
    private readonly IAuthorizationGuard _authGuard;

    public CommentAttachmentsService(
        IStorageService storageService,
        AppDbContext dbContext,
        IAttachmentsRepository<CommentAttachment> attachmentsRepository,
        ICurrentUser user,
        ILogger<AttachmentsService<Comment, CommentAttachment>> logger,
        IAttachmentValidationService attachmentValidationService,
        IOptions<CommentAttachmentOptions> attachmentOptions,
        IAuthorizationGuard authGuard)
        : base(storageService, dbContext, attachmentsRepository, user, logger,
            attachmentValidationService)
    {
        _authGuard = authGuard;
        AttachmentOptions = attachmentOptions.Value;
    }

    protected override AttachmentOptions AttachmentOptions { get; }

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
}
