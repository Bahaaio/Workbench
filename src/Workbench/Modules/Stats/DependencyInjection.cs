using Workbench.Modules.Stats.Repositories;
using Workbench.Modules.Stats.Repositories.Implementations;
using Workbench.Modules.Stats.Services;
using Workbench.Modules.Stats.Services.Implementations;

namespace Workbench.Modules.Stats;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public void AddStatsModule()
        {
            services.AddScoped<IProjectStatsService, ProjectStatsService>();
            services.AddScoped<IProjectStatsRepository, ProjectStatsRepository>();
        }
    }
}
