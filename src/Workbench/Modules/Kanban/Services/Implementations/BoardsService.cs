using Microsoft.EntityFrameworkCore;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Kanban.Dtos;
using Workbench.Modules.Kanban.Mappers;
using Workbench.Modules.Kanban.Models;
using Workbench.Modules.Projects.Models;

namespace Workbench.Modules.Kanban.Services.Implementations;

public class BoardsService : IBoardsService
{
    private readonly AppDbContext _db;

    public BoardsService(AppDbContext dbContext)
    {
        _db = dbContext;
    }

    public async Task<BoardDto> Get(int projectId)
    {
        await _db.Projects.ExistsOrThrowAsync(projectId);
        return await _db.Boards
            .Where(b => b.ProjectId == projectId)
            .Select(BoardMapper.ToDtoExpression)
            .SingleAsync();
    }

    public async Task CreateEmpty(int projectId)
    {
        var board = new Board
        {
            Name = "Board",
            ProjectId = projectId
        };

        _db.Boards.Add(board);
        await _db.SaveChangesAsync();
    }
}
