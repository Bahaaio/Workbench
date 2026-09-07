using Microsoft.EntityFrameworkCore;
using Workbench.Data;
using Workbench.Modules.Kanban.Dtos;
using Workbench.Modules.Kanban.Mappers;
using Workbench.Modules.Kanban.Models;

namespace Workbench.Modules.Kanban.Repositories.Implementations;

public class BoardsRepository : IBoardsRepository
{
    private readonly AppDbContext _dbContext;
    private readonly DbSet<Board> _dbSet;

    public BoardsRepository(AppDbContext context)
    {
        _dbContext = context;
        _dbSet = context.Set<Board>();
    }

    public Board Add(Board entity) => _dbSet.Add(entity).Entity;

    public Board Update(Board entity) => _dbSet.Update(entity).Entity;

    public void Remove(Board entity) => _dbSet.Remove(entity);

    public Task<BoardDto> GetByProjectId(int projectId) =>
        _dbSet
            .Where(b => b.ProjectId == projectId)
            .Select(BoardMapper.ToDtoExpression)
            .SingleAsync();

    public Task<Board> GetByProjectIdRaw(int projectId) =>
        _dbSet
            .Include(b => b.Columns)
            .ThenInclude(c => c.Cards)
            .ThenInclude(c => c.Issue)
            .SingleAsync(b => b.ProjectId == projectId);

    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
