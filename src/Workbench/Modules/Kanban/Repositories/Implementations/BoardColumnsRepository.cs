using Microsoft.EntityFrameworkCore;
using Workbench.Data;
using Workbench.Modules.Kanban.Models;

namespace Workbench.Modules.Kanban.Repositories.Implementations;

public class BoardColumnsRepository : IBoardColumnsRepository
{
    private readonly DbSet<BoardColumn> _dbSet;

    public BoardColumnsRepository(AppDbContext context)
    {
        _dbSet = context.Set<BoardColumn>();
    }

    public BoardColumn Add(BoardColumn entity) => _dbSet.Add(entity).Entity;

    public BoardColumn Update(BoardColumn entity) => _dbSet.Update(entity).Entity;

    public void Remove(BoardColumn entity) => _dbSet.Remove(entity);
}
