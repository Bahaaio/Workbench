using Workbench.Modules.Attachments.Dtos;
using Workbench.Modules.Attachments.Services;
using Workbench.Modules.Milestones.Models;
using Workbench.Modules.Milestones.Options;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;

namespace Workbench.ClientServices.Implementations;

public class MilestoneAttachmentsClient : IMilestoneAttachmentsClient
{
    private readonly IAttachmentsService<Milestone> _attachmentsService;
    private readonly MilestoneAttachmentOptions _options;

    public MilestoneAttachmentsClient(IAttachmentsService<Milestone> attachmentsService,
        IOptions<MilestoneAttachmentOptions> options)
    {
        _attachmentsService = attachmentsService;
        _options = options.Value;
    }

    public async Task<AttachmentDto> Add(int milestoneId, IBrowserFile file)
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

        return await _attachmentsService.Add(milestoneId, formFile);
    }

    public async Task Delete(int milestoneId, Guid attachmentId)
    {
        await _attachmentsService.Delete(attachmentId);
    }
}
