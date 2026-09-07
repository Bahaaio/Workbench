using Workbench.Common.Exceptions;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Kanban.Dtos;
using Workbench.Modules.Kanban.Dtos.Requests;
using Workbench.Modules.Kanban.Mappers;
using Workbench.Modules.Kanban.Models;
using Workbench.Modules.Kanban.Repositories;
using Workbench.Modules.Projects.Repositories;

namespace Workbench.Modules.Kanban.Services.Implementations;

public class BoardCardsService : IBoardCardsService
{
    private const int TempPositionOffset = 1000;
    private readonly IAuthorizationGuard _authGuard;
    private readonly IBoardsRepository _boardsRepository;
    private readonly IBoardCardsRepository _cardsRepository;
    private readonly IProjectsRepository _projectsRepository;
    public BoardCardsService(
        IBoardsRepository boardsRepository,
        IBoardCardsRepository cardsRepository,
        IProjectsRepository projectsRepository,
        IAuthorizationGuard authGuard)
    {
        _boardsRepository = boardsRepository;
        _cardsRepository = cardsRepository;
        _projectsRepository = projectsRepository;
        _authGuard = authGuard;
    }

    public async Task<CardDto> Add(int projectId, CreateCardRequest request)
    {
        await _projectsRepository.ExistsOrThrowAsync(projectId);
        var board = await _boardsRepository.GetByProjectIdRaw(projectId);
        await _authGuard.AuthorizeProjectLead(board);

        if (board.Columns.SelectMany(c => c.Cards).Any(c => c.IssueId == request.IssueId))
            throw new ConflictException($"Issue {request.IssueId} is already on this board");

        var column = board.Columns.FirstOrDefault(c => c.Id == request.ColumnId)
                     ?? throw new NotFoundException(
                         $"Column {request.ColumnId} not found in project {projectId}");

        var maxPosition = column.Cards.Count > 0 ? column.Cards.Max(c => c.Position) : 0;

        var card = new BoardCard
        {
            IssueId = request.IssueId,
            ColumnId = request.ColumnId,
            BoardId = board.Id,
            Position = maxPosition + 1
        };

        _cardsRepository.Add(card);
        await _boardsRepository.SaveChangesAsync();

        await _cardsRepository.LoadIssueAsync(card);
        return card.ToDto();
    }

    public async Task Delete(int projectId, int cardId)
    {
        var project = await _projectsRepository.GetByIdAsync(projectId);
        await _authGuard.AuthorizeProjectLead(project);

        var card = await _cardsRepository.GetByIdAsync(cardId);
        _cardsRepository.Remove(card);

        await _boardsRepository.SaveChangesAsync();
    }

    public async Task<CardDto> Move(int projectId, int cardId, MoveCardRequest request)
    {
        await _projectsRepository.ExistsOrThrowAsync(projectId);
        var board = await _boardsRepository.GetByProjectIdRaw(projectId);
        await _authGuard.AuthorizeProjectLead(board);

        var card = board.Columns
                       .SelectMany(c => c.Cards)
                       .FirstOrDefault(c => c.Id == cardId)
                   ?? throw new NotFoundException($"Card {cardId} not found in project {projectId}");

        var targetColumn = board.Columns.FirstOrDefault(c => c.Id == request.ColumnId)
                           ?? throw new NotFoundException(
                               $"Column {request.ColumnId} not found in project {projectId}");

        var sourceColumn = board.Columns.First(c => c.Cards.Any(x => x.Id == cardId));

        // Remove from source: reorder remaining cards with temp positions
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

        // Move card to target
        card.ColumnId = request.ColumnId;
        card.BoardId = targetColumn.BoardId;

        // Build target order with card inserted at position
        var targetCardIds = targetColumn.Cards
            .OrderBy(c => c.Position)
            .Select(c => c.Id)
            .ToList();

        var insertIndex = Math.Min(request.Position, targetCardIds.Count);
        targetCardIds.Insert(insertIndex, cardId);

        // Set target existing cards to temp positions
        for (var i = 0; i < targetCardIds.Count; i++)
        {
            var c = targetColumn.Cards.FirstOrDefault(x => x.Id == targetCardIds[i]);
            if (c is not null)
                c.Position = TempPositionOffset + i + 1;
            else if (targetCardIds[i] == cardId)
                card.Position = TempPositionOffset + i + 1;
        }

        await _boardsRepository.SaveChangesAsync();

        // Source final positions + target final positions in one save
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

        await _boardsRepository.SaveChangesAsync();

        await _cardsRepository.LoadIssueAsync(card);
        return card.ToDto();
    }

    public async Task Reorder(int projectId, int columnId, ReorderCardsRequest request)
    {
        await _projectsRepository.ExistsOrThrowAsync(projectId);
        var board = await _boardsRepository.GetByProjectIdRaw(projectId);
        await _authGuard.AuthorizeProjectLead(board);

        var column = board.Columns.FirstOrDefault(c => c.Id == columnId)
                     ?? throw new NotFoundException(
                         $"Column {columnId} not found in project {projectId}");

        var ids = request.CardIds;

        // set the positions to a high number to avoid conflicts
        for (var i = 0; i < ids.Count; i++)
        {
            var card = column.Cards.FirstOrDefault(c => c.Id == ids[i]);
            card?.Position = TempPositionOffset + i + 1;
        }

        await _boardsRepository.SaveChangesAsync();

        // reorder again to set the correct positions
        for (var i = 0; i < ids.Count; i++)
        {
            var card = column.Cards.FirstOrDefault(c => c.Id == ids[i]);
            card?.Position = i + 1;
        }

        await _boardsRepository.SaveChangesAsync();
    }
}