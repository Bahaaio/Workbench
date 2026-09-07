using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Projects.Invites.Dtos;
using Workbench.Modules.Projects.Invites.Models;

namespace Workbench.Modules.Projects.Invites.Repositories.Implementations;

public class ProjectInvitesRepository : IProjectInvitesRepository
{
    private readonly DbSet<ProjectInvite> _dbSet;

    public ProjectInvitesRepository(AppDbContext context)
    {
        _dbSet = context.Set<ProjectInvite>();
    }

    public async Task<ProjectInvite?> FindAsync(string code) => await _dbSet.FindAsync(code);

    public Task<ProjectInvite> GetByIdAsync(string code) => _dbSet.FindOrThrowAsync(code);

    public ProjectInvite Add(ProjectInvite entity) => _dbSet.Add(entity).Entity;

    public void Remove(ProjectInvite entity) => _dbSet.Remove(entity);

    public Task ExistsOrThrowAsync(string code) => _dbSet.ExistsOrThrowAsync(code);

    public Task<List<InviteDto>> GetActiveByProjectId(int projectId) =>
        _dbSet
            .Where(i => i.ProjectId == projectId && i.ExpiresAt > DateTime.UtcNow)
            .Select(i => new InviteDto(i.Code, i.ExpiresAt))
            .ToListAsync();
}
