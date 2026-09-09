using Microsoft.Extensions.Options;
using Workbench.Data;
using Workbench.Modules.Attachments.Dtos;
using Workbench.Modules.Attachments.Options;
using Workbench.Modules.Attachments.Services;
using Workbench.Modules.Attachments.Services.Implementations;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Issues.Options;
using Workbench.Modules.Storage.Services;

namespace Workbench.Modules.Issues.Services.Implementations;

public class IssueAttachmentsService : AttachmentsService<Issue, IssueAttachment>
{
    private readonly IAuthorizationGuard _authGuard;
    private readonly IssueAttachmentOptions _options;

    public IssueAttachmentsService(
        IStorageService storageService,
        AppDbContext dbContext,
        ICurrentUser user,
        ILogger<IssueAttachmentsService> logger,
        IAttachmentValidationService attachmentValidationService,
        IAuthorizationGuard authGuard,
        IOptions<IssueAttachmentOptions> attachmentOptions)
        : base(storageService, dbContext, user, logger,
            attachmentValidationService)
    {
        _authGuard = authGuard;
        _options = attachmentOptions.Value;
    }

    protected override AttachmentOptions AttachmentOptions => _options;

    protected override AttachmentOptions GetResolvedOptions(Issue parent) => _options;

    public override async Task<AttachmentDto> Add(int parentId, IFormFile file)
    {
        var issue = await GetOwnerEntity(parentId);
        await _authGuard.AuthorizeOwnerOrProjectMember(issue);

        return await base.Add(parentId, file);
    }

    public override async Task Delete(Guid attachmentId)
    {
        var issue = await GetOwnerEntity(attachmentId);
        await _authGuard.AuthorizeOwnerOrProjectMember(issue);

        await base.Delete(attachmentId);
    }
}
