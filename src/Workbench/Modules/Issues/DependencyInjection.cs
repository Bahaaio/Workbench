using Workbench.Modules.Attachments.Services;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Issues.Options;
using Workbench.Modules.Issues.Services;
using Workbench.Modules.Issues.Services.Implementations;
using Workbench.Modules.Issues.Votes;

namespace Workbench.Modules.Issues;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public void AddIssuesModule()
        {
            services.AddScoped<IIssuesService, IssuesService>();
            services.AddScoped<IIssueTagsService, IssueTagsService>();
            services.AddScoped<IIssueAssignmentsService, IssueAssignmentsService>();
            services.AddScoped<IIssueStatusService, IssueStatusService>();
            services.AddScoped<IAttachmentsService<Issue>, IssueAttachmentsService>();

            services.AddOptions<IssueAttachmentOptions>()
                .BindConfiguration("Attachments:Issues")
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddIssueVotesModule();
        }
    }
}