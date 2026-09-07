using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Attachments.Dtos;
using Workbench.Modules.Attachments.Models;
using Workbench.Modules.Storage.Services;

namespace Workbench.Modules.Attachments.Services.Implementations;

public class AttachmentsReader : IAttachmentsReader
{
    private readonly AppDbContext _db;
    private readonly IStorageService _storageService;

    public AttachmentsReader(IStorageService storageService,
        AppDbContext db)
    {
        _storageService = storageService;
        _db = db;
    }

    public async Task<AttachmentResult> Get(Guid attachmentId)
    {
        var attachment = await _db.Attachments.FindOrThrowAsync(attachmentId);
        var stream = await _storageService.Load(attachmentId.ToString());

        if (stream is null)
            throw new NotFoundException($"Attachment with id: {attachmentId} not found");

        return new AttachmentResult(stream, attachment.ContentType, attachment.OriginalFileName);
    }
}
