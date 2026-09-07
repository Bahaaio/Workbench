using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Projects.Dtos;
using Workbench.Modules.Projects.Mappers;
using Workbench.Modules.Projects.Models;

namespace Workbench.Modules.Projects.Repositories.Implementations;

public class ProjectsRepository : IProjectsRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<Project> _dbSet;

    public ProjectsRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Project>();
    }

    public async Task<Project?> FindAsync(int id) => await _dbSet.FindAsync(id);

    public async Task<Project> GetByIdAsync(int id) =>
        await _dbSet
            .Include(p => p.Owner)
            .SingleOrDefaultAsync(p => p.Id == id)
        ?? throw new NotFoundException($"Project with id {id} not found");

    public Project Add(Project entity) => _dbSet.Add(entity).Entity;

    public Project Update(Project entity) => _dbSet.Update(entity).Entity;

    public void Remove(Project entity) => _dbSet.Remove(entity);

    public Task ExistsOrThrowAsync(int id) => _dbSet.ExistsOrThrowAsync(id);

    public Task<List<ProjectDto>> GetAllAsync() =>
        _dbSet.Select(ProjectMapper.ToDtoExpression).ToListAsync();

    public Task<List<ProjectDto>> GetAllByUserIdAsync(int userId) =>
        _dbSet
            .Where(p => p.OwnerId == userId || p.Members.Any(m => m.UserId == userId))
            .Select(ProjectMapper.ToDtoExpression)
            .ToListAsync();

    public Task LoadOwnerAsync(Project project) =>
        _context.Entry(project).Reference(p => p.Owner).LoadAsync();
}
