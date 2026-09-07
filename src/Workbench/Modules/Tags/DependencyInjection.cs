using Workbench.Modules.Tags.Services;
using Workbench.Modules.Tags.Services.Implementations;

namespace Workbench.Modules.Tags;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public void AddTagsModule()
        {
            services.AddScoped<ITagsService, TagsService>();
        }
    }
}
