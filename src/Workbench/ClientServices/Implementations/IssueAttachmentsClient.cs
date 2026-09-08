using Workbench.Modules.Attachments.Dtos;
using Workbench.Modules.Attachments.Services;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Issues.Options;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;

namespace Workbench.ClientServices.Implementations;

public class IssueAttachmentsClient : IIssueAttachmentsClient
{
    private readonly IAttachmentsService<Issue> _attachmentsService;
    private readonly IssueAttachmentOptions _options;

    public IssueAttachmentsClient(IAttachmentsService<Issue> attachmentsService,
        IOptions<IssueAttachmentOptions> options)
    {
        _attachmentsService = attachmentsService;
        _options = options.Value;
    }

    public async Task<AttachmentDto> Add(int issueId, IBrowserFile file)
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

        return await _attachmentsService.Add(issueId, formFile);
    }

    public async Task Delete(int issueId, Guid attachmentId)
    {
        await _attachmentsService.Delete(attachmentId);
    }
}
