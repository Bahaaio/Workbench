using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Kanban.Dtos;
using Workbench.Modules.Kanban.Dtos.Requests;
using Workbench.Modules.Kanban.Mappers;
using Workbench.Modules.Kanban.Models;

namespace Workbench.Modules.Kanban.Services.Implementations;

public class BoardCardsService : IBoardCardsService
{
    private const int TempPositionOffset = 1000;
    private readonly AppDbContext _db;
    private readonly IAuthorizationGuard _authGuard;

    public BoardCardsService(AppDbContext dbContext, IAuthorizationGuard authGuard)
    {
        _db = dbContext;
        _authGuard = authGuard;
    }

    public async Task<CardDto> Add(int projectId, CreateCardRequest request)
    {
        await _db.Projects.ExistsOrThrowAsync(projectId);
        var board = await GetBoardRaw(projectId);
        await _authGuard.AuthorizeProjectLead(board);

        if (board.Columns.SelectMany(c => c.Cards).Any(c => c.IssueId == request.IssueId))
            throw new ConflictException($"Issue {request.IssueId} is already on this board");

        var issue = await _db.Issues.FindAsync(request.IssueId)
                    ?? throw new NotFoundException($"Issue {request.IssueId} not found");

        if (issue.Status == Status.Closed)
            throw new ConflictException(
                $"Issue {request.IssueId} is closed and cannot be added to the board");

        var column = board.Columns.FirstOrDefault(c => c.Id == request.ColumnId)
                     ?? throw new NotFoundException(
                         $"Column {request.ColumnId} not found in project {projectId}");

        if (column.Cards.Count >= column.MaxCards)
            throw new ConflictException(
                $"Column {column.Id} has reached its maximum card limit of {column.MaxCards}");

        var maxPosition = column.Cards.Count > 0 ? column.Cards.Max(c => c.Position) : 0;

        var card = new BoardCard
        {
            IssueId = request.IssueId,
            ColumnId = request.ColumnId,
            BoardId = board.Id,
            Position = maxPosition + 1
        };

        _db.BoardCards.Add(card);
        await _db.SaveChangesAsync();

        await _db.Entry(card).Reference(c => c.Issue).LoadAsync();
        return card.ToDto();
    }

    public async Task Delete(int projectId, int cardId)
    {
        await _db.Projects.ExistsOrThrowAsync(projectId);
        var board = await GetBoardRaw(projectId);
        await _authGuard.AuthorizeProjectLead(board);

        var card = board.Columns
                       .SelectMany(c => c.Cards)
                       .FirstOrDefault(c => c.Id == cardId)
                   ?? throw new NotFoundException($"Card {cardId} not found in project {projectId}");

        _db.BoardCards.Remove(card);
        await _db.SaveChangesAsync();
    }

    public async Task<CardDto> Move(int projectId, int cardId, MoveCardRequest request)
    {
        await _db.Projects.ExistsOrThrowAsync(projectId);
        var board = await GetBoardRaw(projectId);
        await _authGuard.AuthorizeProjectLead(board);

        var card = board.Columns
                       .SelectMany(c => c.Cards)
                       .FirstOrDefault(c => c.Id == cardId)
                   ?? throw new NotFoundException($"Card {cardId} not found in project {projectId}");

        var targetColumn = board.Columns.FirstOrDefault(c => c.Id == request.ColumnId)
                           ?? throw new NotFoundException(
                               $"Column {request.ColumnId} not found in project {projectId}");

        var sourceColumn = board.Columns.First(c => c.Cards.Any(x => x.Id == cardId));

        if (sourceColumn.Id != targetColumn.Id && targetColumn.Cards.Count >= targetColumn.MaxCards)
            throw new ConflictException(
                $"Column {targetColumn.Id} has reached its maximum card limit of {targetColumn.MaxCards}");

        var sourceCardIds = sourceColumn.Cards
            .Where(c => c.Id != cardId)
            .OrderBy(c => c.Position)
            .Select(c => c.Id)
            .ToList();

        for (var i = 0; i < sourceCardIds.Count; i++)
        {
            var c = sourceColumn.Cards.First(x => x.Id == sourceCardIds[i]);
            c.Position = TempPositionOffset + i + 1;
        }

        card.ColumnId = request.ColumnId;
        card.BoardId = targetColumn.BoardId;

        var targetCardIds = targetColumn.Cards
            .OrderBy(c => c.Position)
            .Select(c => c.Id)
            .ToList();

        var insertIndex = Math.Min(request.Position, targetCardIds.Count);
        targetCardIds.Insert(insertIndex, cardId);

        for (var i = 0; i < targetCardIds.Count; i++)
        {
            var c = targetColumn.Cards.FirstOrDefault(x => x.Id == targetCardIds[i]);
            if (c is not null)
                c.Position = TempPositionOffset + i + 1;
            else if (targetCardIds[i] == cardId)
                card.Position = TempPositionOffset + i + 1;
        }

        await _db.SaveChangesAsync();

        for (var i = 0; i < sourceCardIds.Count; i++)
        {
            var c = sourceColumn.Cards.First(x => x.Id == sourceCardIds[i]);
            c.Position = i + 1;
        }

        for (var i = 0; i < targetCardIds.Count; i++)
        {
            var c = targetColumn.Cards.FirstOrDefault(x => x.Id == targetCardIds[i]);
            if (c is not null)
                c.Position = i + 1;
            else if (targetCardIds[i] == cardId)
                card.Position = i + 1;
        }

        await _db.SaveChangesAsync();

        await _db.Entry(card).Reference(c => c.Issue).LoadAsync();
        return card.ToDto();
    }

    public async Task Reorder(int projectId, int columnId, ReorderCardsRequest request)
    {
        await _db.Projects.ExistsOrThrowAsync(projectId);
        var board = await GetBoardRaw(projectId);
        await _authGuard.AuthorizeProjectLead(board);

        var column = board.Columns.FirstOrDefault(c => c.Id == columnId)
                     ?? throw new NotFoundException(
                         $"Column {columnId} not found in project {projectId}");

        var ids = request.CardIds;

        for (var i = 0; i < ids.Count; i++)
        {
            var card = column.Cards.FirstOrDefault(c => c.Id == ids[i]);
            card?.Position = TempPositionOffset + i + 1;
        }

        await _db.SaveChangesAsync();

        for (var i = 0; i < ids.Count; i++)
        {
            var card = column.Cards.FirstOrDefault(c => c.Id == ids[i]);
            card?.Position = i + 1;
        }

        await _db.SaveChangesAsync();
    }

    private Task<Board> GetBoardRaw(int projectId) =>
        _db.Boards
            .Include(b => b.Columns)
            .ThenInclude(c => c.Cards)
            .ThenInclude(c => c.Issue)
            .SingleAsync(b => b.ProjectId == projectId);
}
