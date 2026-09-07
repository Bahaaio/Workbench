using Workbench.Common.Extensions;
using Workbench.Modules.Attachments.Services;
using Workbench.Modules.Milestones.Models;
using Workbench.Modules.Milestones.Options;
using Workbench.Modules.Milestones.Services;
using Workbench.Modules.Milestones.Services.Implementations;

namespace Workbench.Modules.Milestones;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public void AddMilestonesModule()
        {
            services.AddScoped<IMilestonesService, MilestonesService>();
            services.AddScoped<IMilestoneIssuesService, MilestoneIssuesService>();
            services.AddScoped<IAttachmentsService<Milestone>, MilestoneAttachmentsService>();
            services.AddKeyableOptions<MilestoneAttachmentOptions>();
        }
    }
}
