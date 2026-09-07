using Workbench.Data;
using Workbench.Modules.Issues.Dtos;
using Workbench.Modules.Issues.Mappers;
using Workbench.Modules.Issues.Models;
using Microsoft.EntityFrameworkCore;

namespace Workbench.Modules.Issues.Repositories.Implementations;

public class IssueStatusChangeRepository : IIssueStatusChangeRepository
{
    private readonly DbSet<IssueStatusChange> _dbSet;

    public IssueStatusChangeRepository(AppDbContext context)
    {
        _dbSet = context.Set<IssueStatusChange>();
    }

    public IssueStatusChange Add(IssueStatusChange entity) => _dbSet.Add(entity).Entity;

    public Task<List<StatusChangeDto>> GetHistoryAsync(int issueId) =>
        _dbSet
            .AsNoTracking()
            .Where(s => s.IssueId == issueId)
            .OrderBy(s => s.ChangedAt)
            .Select(StatusChangeMapper.ToDtoExpression)
            .ToListAsync();
}
