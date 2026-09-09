using Workbench.Modules.Attachments.Dtos;
using Workbench.Modules.Attachments.Services;
using Workbench.Modules.Milestones.Models;
using Microsoft.AspNetCore.Mvc;

namespace Workbench.Modules.Milestones.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/milestones/{milestoneId:int}/attachments")]
public class MilestoneAttachmentsController : ControllerBase
{
    private readonly IAttachmentsService<Milestone> _attachmentsService;

    public MilestoneAttachmentsController(IAttachmentsService<Milestone> attachmentsService)
    {
        _attachmentsService = attachmentsService;
    }

    [HttpPost]
    public async Task<ActionResult<AttachmentDto>> Attach(int milestoneId, IFormFile file) =>
        Ok(await _attachmentsService.Add(milestoneId, file));

    [HttpDelete("{attachmentId:guid}")]
    public async Task<ActionResult> Delete(int milestoneId, Guid attachmentId)
    {
        await _attachmentsService.Delete(attachmentId);
        return NoContent();
    }
}
