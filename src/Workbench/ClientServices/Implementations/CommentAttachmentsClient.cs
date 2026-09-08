using Workbench.Modules.Attachments.Dtos;
using Workbench.Modules.Attachments.Services;
using Workbench.Modules.Comments.Models;
using Workbench.Modules.Comments.Options;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;

namespace Workbench.ClientServices.Implementations;

public class CommentAttachmentsClient : ICommentAttachmentsClient
{
    private readonly IAttachmentsService<Comment> _attachmentsService;
    private readonly CommentAttachmentOptions _options;

    public CommentAttachmentsClient(IAttachmentsService<Comment> attachmentsService,
        IOptions<CommentAttachmentOptions> options)
    {
        _attachmentsService = attachmentsService;
        _options = options.Value;
    }

    public async Task<AttachmentDto> Add(int commentId, IBrowserFile file)
    {
        await using var source = file.OpenReadStream(_options.MaxSizeBytesLead);
        await using var stream = new MemoryStream();
        await source.CopyToAsync(stream);
        stream.Position = 0;

        var formFile = new FormFile(stream, 0, stream.Length, "file", file.Name)
        {
            Headers = new HeaderDictionary(),
            ContentType = file.ContentType ?? "application/octet-stream"
        };

        return await _attachmentsService.Add(commentId, formFile);
    }

    public async Task Delete(int commentId, Guid attachmentId)
    {
        await _attachmentsService.Delete(attachmentId);
    }
}
