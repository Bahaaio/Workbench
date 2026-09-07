using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Issues.Dtos;
using Workbench.Modules.Issues.Mappers;
using Workbench.Modules.Milestones.Dtos;
using Workbench.Modules.Milestones.Mappers;
using Workbench.Modules.Milestones.Models;

namespace Workbench.Modules.Milestones.Repositories.Implementations;

public class MilestonesRepository : IMilestonesRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<Milestone> _dbSet;

    public MilestonesRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Milestone>();
    }

    public async Task<Milestone?> FindAsync(int id) => await _dbSet.FindAsync(id);

    public async Task<Milestone> GetByIdAsync(int id) =>
        await _dbSet
            .Include(m => m.MilestoneItems)
                .ThenInclude(mi => mi.Issue)
            .SingleOrDefaultAsync(m => m.Id == id)
        ?? throw new NotFoundException($"Milestone with id {id} not found");

    public Milestone Add(Milestone entity) => _dbSet.Add(entity).Entity;

    public Milestone Update(Milestone entity) => _dbSet.Update(entity).Entity;

    public void Remove(Milestone entity) => _dbSet.Remove(entity);

    public Task<List<MilestoneDto>> GetAllAsync(int projectId) =>
        _dbSet
            .Where(m => m.ProjectId == projectId)
            .Include(m => m.MilestoneItems)
                .ThenInclude(mi => mi.Issue)
            .Select(MilestoneMapper.ToDtoExpression)
            .ToListAsync();

    public Task<Milestone?> FindForUpdateAsync(int milestoneId) =>
        _dbSet
            .Include(m => m.MilestoneItems)
            .SingleOrDefaultAsync(m => m.Id == milestoneId);

    public Task<Milestone?> FindWithItemsAsync(int milestoneId) =>
        _dbSet
            .Include(m => m.MilestoneItems)
                .ThenInclude(mi => mi.Issue)
            .SingleOrDefaultAsync(m => m.Id == milestoneId);

    public Task<List<IssueDto>> GetAllIssuesAsync(int milestoneId) =>
        _context.Set<MilestoneItem>()
            .AsNoTracking()
            .Where(mi => mi.MilestoneId == milestoneId)
            .Select(mi => mi.Issue)
            .Select(IssueMapper.ToDtoExpression)
            .ToListAsync();
}
