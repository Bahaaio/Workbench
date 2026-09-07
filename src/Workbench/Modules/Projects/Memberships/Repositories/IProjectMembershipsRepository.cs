using Workbench.Modules.Projects.Memberships.Dtos;
using Workbench.Modules.Projects.Memberships.Models;

namespace Workbench.Modules.Projects.Memberships.Repositories;

public interface IProjectMembershipsRepository
{
    Task<ProjectMembership?> FindMembershipByProjectIdAndUserId(int projectId, int userId);
    Task<ProjectMembership> GetByProjectIdAndUsernameAsync(int projectId, string username);
    Task<List<ProjectMembershipDto>> GetMembershipsByProjectId(int projectId);
    void Add(ProjectMembership membership);
    void Remove(ProjectMembership membership);
    Task SaveChangesAsync();
}