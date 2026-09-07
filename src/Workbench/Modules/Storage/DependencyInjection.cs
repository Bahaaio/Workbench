using Workbench.Modules.Storage.Services;
using Workbench.Modules.Storage.Services.Implementations;

namespace Workbench.Modules.Storage;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public void AddStorageModule()
        {
            services.AddScoped<IStorageService, LocalStorageService>();
            services.AddKeyedScoped<IStorageService, LocalStorageService>("local");
            services.AddKeyedScoped<IStorageService, CloudStorageService>("cloud");
        }
    }
}
