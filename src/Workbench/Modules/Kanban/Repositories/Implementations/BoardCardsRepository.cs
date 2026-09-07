using Microsoft.EntityFrameworkCore;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Kanban.Models;

namespace Workbench.Modules.Kanban.Repositories.Implementations;

public class BoardCardsRepository : IBoardCardsRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<BoardCard> _dbSet;

    public BoardCardsRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<BoardCard>();
    }

    public async Task<BoardCard?> FindAsync(int id) => await _dbSet.FindAsync(id);

    public Task<BoardCard> GetByIdAsync(int id) => _dbSet.FindOrThrowAsync(id);

    public BoardCard Add(BoardCard entity) => _dbSet.Add(entity).Entity;

    public BoardCard Update(BoardCard entity) => _dbSet.Update(entity).Entity;

    public void Remove(BoardCard entity) => _dbSet.Remove(entity);

    public Task LoadIssueAsync(BoardCard card) =>
        _context.Entry(card).Reference(c => c.Issue).LoadAsync();

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
