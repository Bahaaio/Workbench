using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Comments.Dtos;
using Workbench.Modules.Comments.Mappers;
using Workbench.Modules.Comments.Models;
using Workbench.Modules.Issues.Models;

namespace Workbench.Modules.Comments.Repositories.Implementations;

public class CommentsRepository : ICommentsRepository
{
    private readonly AppDbContext _dbContext;
    private readonly DbSet<Comment> _dbSet;
    private readonly DbSet<Issue> _issues;

    public CommentsRepository(AppDbContext context)
    {
        _dbContext = context;
        _dbSet = context.Set<Comment>();
        _issues = context.Set<Issue>();
    }

    public async Task<Comment> GetByIdAsync(int id) =>
        await _dbSet
            .Where(c => c.Id == id)
            .Include(c => c.Author)
            .Include(c => c.Attachments)
            .Include(c => c.Issue).ThenInclude(i => i.Project)
            .SingleOrDefaultAsync()
        ?? throw new NotFoundException($"Comment with id: {id} not found");

    public Comment Add(Comment entity) => _dbSet.Add(entity).Entity;

    public void Remove(Comment entity) => _dbSet.Remove(entity);

    public async Task<List<CommentDto>> GetAllByIssueIdAsync(int issueId)
    {
        await _issues.ExistsOrThrowAsync(issueId);

        return await _dbSet
            .AsNoTracking()
            .Where(c => c.IssueId == issueId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(CommentMapper.ToDtoExpression)
            .ToListAsync();
    }

    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
