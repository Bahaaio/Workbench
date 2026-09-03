using Workbench.Modules.Stats.Dtos;

namespace Workbench.Modules.Stats.Services;

public interface IProjectStatsService
{
    Task<ProjectStatsDto> GetProjectStatsAsync(int projectId);
}
