using Workbench.Modules.Kanban.Models;

namespace Workbench.Modules.Kanban.Repositories;

public interface IBoardCardsRepository
{
    Task<BoardCard?> FindAsync(int id);
    Task<BoardCard> GetByIdAsync(int id);
    BoardCard Add(BoardCard entity);
    BoardCard Update(BoardCard entity);
    void Remove(BoardCard entity);
    Task LoadIssueAsync(BoardCard card);
    Task SaveChangesAsync();
}
