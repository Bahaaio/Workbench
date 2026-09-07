using Microsoft.Extensions.Options;
using Workbench.Data;
using Workbench.Modules.Attachments.Dtos;
using Workbench.Modules.Attachments.Options;
using Workbench.Modules.Attachments.Services;
using Workbench.Modules.Attachments.Services.Implementations;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Milestones.Models;
using Workbench.Modules.Milestones.Options;
using Workbench.Modules.Storage.Services;

namespace Workbench.Modules.Milestones.Services.Implementations;

public class MilestoneAttachmentsService : AttachmentsService<Milestone, MilestoneAttachment>
{
    private readonly IAuthorizationGuard _authGuard;

    public MilestoneAttachmentsService(
        [FromKeyedServices("cloud")] IStorageService storageService,
        AppDbContext dbContext,
        ICurrentUser user,
        ILogger<MilestoneAttachmentsService> logger,
        IAttachmentValidationService attachmentValidationService,
        IAuthorizationGuard authGuard,
        IOptions<MilestoneAttachmentOptions> options)
        : base(storageService, dbContext, user, logger, attachmentValidationService)
    {
        _authGuard = authGuard;
        AttachmentOptions = options.Value;
    }

    protected override AttachmentOptions AttachmentOptions { get; }

    public override async Task<AttachmentDto> Add(int parentId, IFormFile file)
    {
        var milestone = await GetOwnerEntity(parentId);
        await _authGuard.AuthorizeProjectLead(milestone);
        return await base.Add(parentId, file);
    }

    public override async Task Delete(Guid attachmentId)
    {
        var milestone = await GetOwnerEntity(attachmentId);
        await _authGuard.AuthorizeProjectLead(milestone);
        await base.Delete(attachmentId);
    }
}
