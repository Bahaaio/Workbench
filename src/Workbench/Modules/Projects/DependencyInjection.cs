using Workbench.Modules.Projects.Invites;
using Workbench.Modules.Projects.Memberships;
using Workbench.Modules.Projects.Services;
using Workbench.Modules.Projects.Services.Implementations;

namespace Workbench.Modules.Projects;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public void AddProjectsModule()
        {
            services.AddScoped<IProjectsService, ProjectsService>();

            services.AddProjectMembershipsModule();
            services.AddProjectInvitesModule();
        }
    }
}
