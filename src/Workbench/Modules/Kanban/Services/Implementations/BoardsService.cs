using Workbench.Modules.Kanban.Dtos;
using Workbench.Modules.Kanban.Models;
using Workbench.Modules.Kanban.Repositories;
using Workbench.Modules.Projects.Repositories;

namespace Workbench.Modules.Kanban.Services.Implementations;

public class BoardsService : IBoardsService
{
    private readonly IBoardsRepository _boardsRepository;
    private readonly IProjectsRepository _projectsRepository;

    public BoardsService(IBoardsRepository boardsRepository, IProjectsRepository projectsRepository)
    {
        _boardsRepository = boardsRepository;
        _projectsRepository = projectsRepository;
    }

    public async Task<BoardDto> Get(int projectId)
    {
        await _projectsRepository.ExistsOrThrowAsync(projectId);
        return await _boardsRepository.GetByProjectId(projectId);
    }

    public async Task CreateEmpty(int projectId)
    {
        var board = new Board
        {
            Name = "Board",
            ProjectId = projectId
        };

        _boardsRepository.Add(board);
        await _boardsRepository.SaveChangesAsync();
    }
}