using Workbench.Modules.Projects.Dtos;
using Workbench.Modules.Projects.Models;

namespace Workbench.Modules.Projects.Repositories;

public interface IProjectsRepository
{
    Task<Project?> FindAsync(int id);
    Task<Project> GetByIdAsync(int id);
    Project Add(Project entity);
    Project Update(Project entity);
    void Remove(Project entity);
    Task ExistsOrThrowAsync(int id);
    Task<List<ProjectDto>> GetAllAsync();
    Task<List<ProjectDto>> GetAllByUserIdAsync(int userId);
    Task LoadOwnerAsync(Project project);
}
