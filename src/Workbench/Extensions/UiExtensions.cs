using Workbench.ClientServices;
using Workbench.ClientServices.Implementations;
using MudBlazor.Services;

namespace Workbench.Extensions;

public static class UiExtensions
{
    extension(IServiceCollection services)
    {
        public void AddUiServices()
        {
            services.AddRazorComponents().AddInteractiveServerComponents();
            services.AddMudServices();
            services.AddScoped<IAuthState, AuthState>();
            services.AddScoped<IProjectMembershipState, ProjectMembershipState>();
            services.AddScoped<IIssueAttachmentsClient, IssueAttachmentsClient>();
            services.AddScoped<ICommentAttachmentsClient, CommentAttachmentsClient>();
            services.AddScoped<IMilestoneAttachmentsClient, MilestoneAttachmentsClient>();
        }
    }
}
