using Microsoft.Extensions.Options;
using Workbench.Data;
using Workbench.Data.Persistence;
using Workbench.Modules.Attachments.Dtos;
using Workbench.Modules.Attachments.Options;
using Workbench.Modules.Attachments.Repositories;
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

    public IssueAttachmentsService(
        IStorageService storageService,
        AppDbContext dbContext,
        IAttachmentsRepository<IssueAttachment> attachmentsRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser user,
        ILogger<IssueAttachmentsService> logger,
        IAttachmentValidationService attachmentValidationService,
        IAuthorizationGuard authGuard,
        IOptions<IssueAttachmentOptions> attachmentOptions)
        : base(storageService, dbContext, attachmentsRepository, unitOfWork, user, logger,
            attachmentValidationService)
    {
        _authGuard = authGuard;
        AttachmentOptions = attachmentOptions.Value;
    }

    protected override AttachmentOptions AttachmentOptions { get; }

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
