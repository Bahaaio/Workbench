using Workbench.Modules.Kanban.Dtos;
using Workbench.Modules.Kanban.Models;

namespace Workbench.Modules.Kanban.Repositories;

public interface IBoardsRepository
{
    Board Add(Board entity);
    Board Update(Board entity);
    void Remove(Board entity);
    Task<BoardDto> GetByProjectId(int projectId);
    Task<Board> GetByProjectIdRaw(int projectId);
}
