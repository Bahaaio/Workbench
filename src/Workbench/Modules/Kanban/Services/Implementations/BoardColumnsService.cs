using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Kanban.Dtos;
using Workbench.Modules.Kanban.Dtos.Requests;
using Workbench.Modules.Kanban.Mappers;
using Workbench.Modules.Kanban.Models;

namespace Workbench.Modules.Kanban.Services.Implementations;

public class BoardColumnsService : IBoardColumnsService
{
    private const int TempPositionOffset = 1000;
    private readonly IAuthorizationGuard _authGuard;
    private readonly AppDbContext _db;

    public BoardColumnsService(AppDbContext dbContext, IAuthorizationGuard authGuard)
    {
        _db = dbContext;
        _authGuard = authGuard;
    }

    public async Task<ColumnDto> Add(int projectId, CreateColumnRequest request)
    {
        await _db.Projects.ExistsOrThrowAsync(projectId);
        var board = await GetBoardRaw(projectId);
        await _authGuard.AuthorizeProjectLead(board);

        var maxPosition = board.Columns.Count > 0 ? board.Columns.Max(c => c.Position) : 0;

        var column = new BoardColumn
        {
            Name = request.Name,
            Description = request.Description,
            Color = request.Color,
            Position = maxPosition + 1,
            MaxCards = request.MaxCards,
            BoardId = board.Id
        };

        _db.BoardColumns.Add(column);
        await _db.SaveChangesAsync();

        return column.ToDto();
    }

    public async Task<ColumnDto> Update(int projectId, int columnId, UpdateColumnRequest request)
    {
        var column = await GetColumnForProject(projectId, columnId);

        column.Name = request.Name;
        column.Description = request.Description;
        column.Color = request.Color;
        column.MaxCards = request.MaxCards;

        await _db.SaveChangesAsync();

        return column.ToDto();
    }

    public async Task Delete(int projectId, int columnId)
    {
        var column = await GetColumnForProject(projectId, columnId);

        _db.BoardColumns.Remove(column);
        await _db.SaveChangesAsync();
    }

    public async Task Reorder(int projectId, MoveColumnRequest request)
    {
        await _db.Projects.ExistsOrThrowAsync(projectId);
        var board = await GetBoardRaw(projectId);
        await _authGuard.AuthorizeProjectLead(board);

        var ids = request.ColumnIds;

        for (var i = 0; i < ids.Count; i++)
        {
            var column = board.Columns.FirstOrDefault(c => c.Id == ids[i]);
            column?.Position = TempPositionOffset + i + 1;
        }

        await _db.SaveChangesAsync();

        for (var i = 0; i < ids.Count; i++)
        {
            var column = board.Columns.FirstOrDefault(c => c.Id == ids[i]);
            column?.Position = i + 1;
        }

        await _db.SaveChangesAsync();
    }

    private async Task<BoardColumn> GetColumnForProject(int projectId, int columnId)
    {
        await _db.Projects.ExistsOrThrowAsync(projectId);
        var board = await GetBoardRaw(projectId);
        await _authGuard.AuthorizeProjectLead(board);

        return board.Columns.FirstOrDefault(c => c.Id == columnId)
               ?? throw new NotFoundException(
                   $"Column {columnId} not found in project {projectId}");
    }

    private Task<Board> GetBoardRaw(int projectId) =>
        _db.Boards
            .Include(b => b.Columns)
            .ThenInclude(c => c.Cards)
            .ThenInclude(c => c.Issue)
            .SingleAsync(b => b.ProjectId == projectId);
}