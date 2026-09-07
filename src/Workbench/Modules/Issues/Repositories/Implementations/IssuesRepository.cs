using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Issues.Dtos;
using Workbench.Modules.Issues.Dtos.Requests;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Issues.Extensions;
using Workbench.Modules.Issues.Mappers;
using Workbench.Modules.Issues.Models;

namespace Workbench.Modules.Issues.Repositories.Implementations;

public class IssuesRepository : IIssuesRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<Issue> _dbSet;

    public IssuesRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Issue>();
    }

    public async Task<Issue?> FindAsync(int id) => await _dbSet.FindAsync(id);

    public async Task<Issue> GetByIdAsync(int id) =>
        await _dbSet
            .Where(i => i.Id == id)
            .Include(i => i.Project)
            .Include(i => i.Author)
            .Include(i => i.AssignedTo)
            .Include(i => i.Tags)
            .Include(i => i.Attachments)
            .Include(i => i.Votes)
            .AsSplitQuery()
            .SingleOrDefaultAsync()
        ?? throw new NotFoundException($"Issue with id {id} not found");

    public Issue Add(Issue entity) => _dbSet.Add(entity).Entity;

    public Issue Update(Issue entity) => _dbSet.Update(entity).Entity;

    public void Remove(Issue entity) => _dbSet.Remove(entity);

    public Task ExistsOrThrowAsync(int id) => _dbSet.ExistsOrThrowAsync(id);

    public Task<List<IssueDto>> GetAllAsync(int projectId, IssueQuery query) =>
        _dbSet
            .AsNoTracking()
            .Where(i => i.ProjectId == projectId)
            .ApplyFilters(query)
            .Select(IssueMapper.ToDtoExpression)
            .ToListAsync();

    public Task<Issue?> FindForUpdateAsync(int id) =>
        _dbSet
            .Where(i => i.Id == id)
            .Include(i => i.Author)
            .Include(i => i.AssignedTo)
            .Include(i => i.Tags)
            .Include(i => i.Votes)
            .AsSplitQuery()
            .SingleOrDefaultAsync();

    public Task<Issue?> FindWithTagsAsync(int id) =>
        _dbSet
            .Where(i => i.Id == id)
            .Include(i => i.Tags)
            .SingleOrDefaultAsync();

    public Task<List<IssueDto>> GetAllByAuthorAsync(int authorId, IssueQuery query) =>
        _dbSet
            .AsNoTracking()
            .ApplyFilters(query)
            .Where(i => i.AuthorId == authorId)
            .Select(IssueMapper.ToDtoExpression)
            .ToListAsync();

    public Task<List<IssueDto>> GetAllAssignedToUserAsync(int userId, IssueQuery query) =>
        _dbSet
            .AsNoTracking()
            .ApplyFilters(query)
            .Where(i => i.AssignedToId == userId)
            .Select(IssueMapper.ToDtoExpression)
            .ToListAsync();

    public Task LoadAuthorAsync(Issue issue) =>
        _context.Entry(issue).Reference(i => i.Author).LoadAsync();

    public async Task UnassignFromAllAsync(int projectId, int userId)
    {
        await _dbSet
            .Where(i =>
                i.ProjectId == projectId &&
                i.AssignedToId == userId &&
                i.Status != Status.Closed)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(i => i.AssignedToId, (int?)null));
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
