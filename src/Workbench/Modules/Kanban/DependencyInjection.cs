using Workbench.Modules.Kanban.Services;
using Workbench.Modules.Kanban.Services.Implementations;

namespace Workbench.Modules.Kanban;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public void AddKanbanModule()
        {
            services.AddScoped<IBoardsService, BoardsService>();
            services.AddScoped<IBoardColumnsService, BoardColumnsService>();
            services.AddScoped<IBoardCardsService, BoardCardsService>();
        }
    }
}
