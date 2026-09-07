using Workbench.Common.Options;
using Workbench.Modules.Attachments.Options;

namespace Workbench.Modules.Milestones.Options;

public class MilestoneAttachmentOptions : AttachmentOptions, IKeyableOptions
{
    public static string Key => $"{BaseKey}:Milestones";
}
