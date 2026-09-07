using Workbench.Modules.Kanban.Models;

namespace Workbench.Modules.Kanban.Repositories;

public interface IBoardColumnsRepository
{
    BoardColumn Add(BoardColumn entity);
    BoardColumn Update(BoardColumn entity);
    void Remove(BoardColumn entity);
    Task SaveChangesAsync();
}
