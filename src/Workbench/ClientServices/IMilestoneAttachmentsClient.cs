using Workbench.Modules.Attachments.Dtos;
using Microsoft.AspNetCore.Components.Forms;

namespace Workbench.ClientServices;

public interface IMilestoneAttachmentsClient
{
    Task<AttachmentDto> Add(int milestoneId, IBrowserFile file);
    Task Delete(int milestoneId, Guid attachmentId);
}
