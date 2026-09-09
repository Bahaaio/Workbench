using Microsoft.EntityFrameworkCore;
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
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Storage.Services;

namespace Workbench.Modules.Milestones.Services.Implementations;

public class MilestoneAttachmentsService : AttachmentsService<Milestone, MilestoneAttachment>
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationGuard _authGuard;
    private readonly ICurrentUser _user;
    private readonly MilestoneAttachmentOptions _options;

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
        _db = dbContext;
        _authGuard = authGuard;
        _user = user;
        _options = options.Value;
    }

    protected override AttachmentOptions AttachmentOptions => _options;

    protected override AttachmentOptions GetResolvedOptions(Milestone parent)
    {
        var isLead = _db.ProjectMemberships
            .Any(m => m.ProjectId == parent.ProjectId && m.UserId == _user.Id
                && m.Role == ProjectMemberRole.Lead);

        return new MilestoneAttachmentOptions
        {
            MaxSizeBytes = isLead ? _options.MaxSizeBytesLead : _options.MaxSizeBytes,
            MaxCount = _options.MaxCount,
            AllowedExtensions = _options.AllowedExtensions,
            MaxSizeBytesLead = _options.MaxSizeBytesLead
        };
    }

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
