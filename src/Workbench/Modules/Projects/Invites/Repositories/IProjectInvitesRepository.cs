using Workbench.Modules.Projects.Invites.Dtos;
using Workbench.Modules.Projects.Invites.Models;

namespace Workbench.Modules.Projects.Invites.Repositories;

public interface IProjectInvitesRepository
{
    Task<ProjectInvite?> FindAsync(string code);
    Task<ProjectInvite> GetByIdAsync(string code);
    ProjectInvite Add(ProjectInvite entity);
    void Remove(ProjectInvite entity);
    Task ExistsOrThrowAsync(string code);
    Task<List<InviteDto>> GetActiveByProjectId(int projectId);
}
