using Microsoft.EntityFrameworkCore;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Tags.Dtos;
using Workbench.Modules.Tags.Mappers;
using Workbench.Modules.Tags.Models;

namespace Workbench.Modules.Tags.Repositories.Implementations;

public class TagsRepository : ITagsRepository
{
    private readonly AppDbContext _dbContext;
    private readonly DbSet<Tag> _dbSet;

    public TagsRepository(AppDbContext context)
    {
        _dbContext = context;
        _dbSet = context.Set<Tag>();
    }

    public async Task<Tag?> FindAsync(int id) => await _dbSet.FindAsync(id);

    public Task<Tag> GetByIdAsync(int id) => _dbSet.FindOrThrowAsync(id);

    public Tag Add(Tag entity) => _dbSet.Add(entity).Entity;

    public Tag Update(Tag entity) => _dbSet.Update(entity).Entity;

    public void Remove(Tag entity) => _dbSet.Remove(entity);

    public Task<List<TagDto>> GetAllByProjectIdAsync(int projectId) =>
        _dbSet
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .Select(TagMapper.ToDtoExpression)
            .ToListAsync();

    public Task<Tag?> FindByNameAsync(int projectId, string name) =>
        _dbSet
            .Where(t => t.ProjectId == projectId)
            .SingleOrDefaultAsync(t => EF.Functions.ILike(t.Name, name));

    public Task<List<Tag>> GetByNamesAsync(int projectId, IEnumerable<string> names) =>
        _dbSet
            .Where(t => t.ProjectId == projectId)
            .Where(t => names.Contains(t.Name))
            .ToListAsync();

    public Task<int> DeleteByNameAsync(int projectId, string name) =>
        _dbSet
            .Where(t => t.ProjectId == projectId)
            .Where(t => EF.Functions.ILike(t.Name, name))
            .ExecuteDeleteAsync();

    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
