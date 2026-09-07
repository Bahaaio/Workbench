using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Common.Extensions;
using Workbench.Common.Models;
using Workbench.Data;
using Workbench.Modules.Attachments.Dtos;
using Workbench.Modules.Attachments.Mappers;
using Workbench.Modules.Attachments.Models;
using Workbench.Modules.Attachments.Options;
using Workbench.Modules.Attachments.Repositories;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Models;
using Workbench.Modules.Storage.Services;

namespace Workbench.Modules.Attachments.Services.Implementations;

/// <summary>
///     Generic attachment service.
/// </summary>
/// <remarks>
///     This class does not implement any authorization logic.
///     You must implement authorization logic in the derived classes.
/// </remarks>
/// <typeparam name="TParent">The parent resource that owns attachments.</typeparam>
/// <typeparam name="TAttachment">The attachment type.</typeparam>
public abstract class AttachmentsService<TParent, TAttachment> : IAttachmentsService<TParent>
    where TParent : class, IOwnedByUser, IEntity<int>
    where TAttachment : Attachment, IHasParent<TParent>, new()
{
    private readonly IAttachmentValidationService _attachmentValidationService;
    private readonly IAttachmentsRepository<TAttachment> _attachmentsRepository;
    private readonly ILogger<AttachmentsService<TParent, TAttachment>> _logger;
    private readonly DbSet<TParent> _parentSet;
    private readonly IStorageService _storageService;
    private readonly ICurrentUser _user;

    protected AttachmentsService(
        IStorageService storageService,
        AppDbContext dbContext,
        IAttachmentsRepository<TAttachment> attachmentsRepository,
        ICurrentUser user,
        ILogger<AttachmentsService<TParent, TAttachment>> logger,
        IAttachmentValidationService attachmentValidationService)
    {
        _storageService = storageService;
        _parentSet = dbContext.Set<TParent>();
        _attachmentsRepository = attachmentsRepository;
        _user = user;
        _logger = logger;
        _attachmentValidationService = attachmentValidationService;
    }

    protected abstract AttachmentOptions AttachmentOptions { get; }

    public virtual async Task<AttachmentDto> Add(int parentId, IFormFile file)
    {
        _attachmentValidationService.Validate(file, AttachmentOptions);
        await _parentSet.ExistsOrThrowAsync(parentId);

        var count = await _attachmentsRepository.CountByParentIdAsync(parentId);
        _attachmentValidationService.ValidateCount(count + 1, AttachmentOptions.MaxCount);

        var guid = Guid.NewGuid();
        await _storageService.Store(file, guid.ToString());

        var attachment = new TAttachment
        {
            Id = guid,
            ParentId = parentId,
            ContentType = file.ContentType,
            OriginalFileName = file.FileName,
            UploaderId = _user.Id
        };

        _attachmentsRepository.Add(attachment);
        await _attachmentsRepository.SaveChangesAsync();

        _logger.LogInformation(
            "User {userId} added attachment {attachmentId} to parent {ParentId}",
            _user.Id, attachment.Id, parentId);

        return attachment.ToDto();
    }

    public virtual async Task Delete(Guid attachmentId)
    {
        var attachment = await _attachmentsRepository.GetByIdAsync(attachmentId);

        _attachmentsRepository.Remove(attachment);
        await _storageService.DeleteFile(attachmentId.ToString());
        await _attachmentsRepository.SaveChangesAsync();

        _logger.LogInformation("User {userId} deleted attachment {attachmentId}",
            _user.Id, attachmentId);
    }

    public virtual async Task DeleteAll(int parentId)
    {
        var keys = await _attachmentsRepository.GetIdsByParentIdAsync(parentId);

        foreach (var key in keys)
            await _storageService.DeleteFile(key);
    }

    /// <summary>
    ///     Gets the owner entity of an attachment by parent ID.
    ///     Used to implement authorization logic in derived classes.
    /// </summary>
    protected Task<TParent> GetOwnerEntity(int parentId) =>
        _parentSet.FindOrThrowAsync(parentId);

    /// <summary>
    ///     Gets the owner entity of an attachment by attachment ID.
    ///     Used to implement authorization logic in derived classes.
    /// </summary>
    protected async Task<TParent> GetOwnerEntity(Guid attachmentId)
    {
        var parentId = await _attachmentsRepository.GetParentIdByAttachmentAsync(attachmentId);
        return await _parentSet.FindOrThrowAsync(parentId);
    }
}
