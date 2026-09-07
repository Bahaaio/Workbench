using Microsoft.AspNetCore.Authorization;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Kanban.Dtos.Requests;
using Workbench.Modules.Kanban.Models;
using Workbench.Modules.Kanban.Repositories;
using Workbench.Modules.Kanban.Services.Implementations;
using Workbench.Modules.Projects.Models;
using Workbench.Modules.Projects.Repositories;

namespace Workbench.Tests.Services.Kanban;

public class BoardCardsServiceTests
{
    private const int ProjectId = 1;
    private const int BoardId = 10;
    private const int ColumnId = 20;
    private const int CardId = 30;
    private const int IssueId = 100;

    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly Mock<IBoardsRepository> _boardsRepo;
    private readonly Mock<IBoardCardsRepository> _cardsRepo;
    private readonly Mock<IProjectsRepository> _projectsRepo;
    private readonly BoardCardsService _service;

    public BoardCardsServiceTests()
    {
        _authGuard = new Mock<IAuthorizationGuard>();
        _boardsRepo = new Mock<IBoardsRepository>();
        _cardsRepo = new Mock<IBoardCardsRepository>();
        _projectsRepo = new Mock<IProjectsRepository>();

        _service = new BoardCardsService(
            _boardsRepo.Object,
            _cardsRepo.Object,
            _projectsRepo.Object,
            _authGuard.Object);
    }

    private static Board MakeBoard(List<BoardColumn>? columns = null) =>
        new()
        {
            Id = BoardId,
            Name = "Board",
            ProjectId = ProjectId,
            Columns = columns ?? [],
            Project = new Project
            {
                Id = ProjectId,
                OwnerId = 1,
                Name = "P",
                Description = null,
                Owner = new Modules.Auth.Models.ApplicationUser { Id = 1, UserName = "u" }
            }
        };

    private static BoardColumn MakeColumn(int id = ColumnId, List<BoardCard>? cards = null) =>
        new()
        {
            Id = id,
            Name = "Col",
            Description = null,
            Position = 1,
            Color = Common.Enums.Color.Blue,
            BoardId = BoardId,
            Board = null!,
            Cards = cards ?? []
        };

    private static BoardCard MakeCard(int id = CardId, int columnId = ColumnId, int issueId = IssueId, int position = 1) =>
        new()
        {
            Id = id,
            Position = position,
            BoardId = BoardId,
            ColumnId = columnId,
            IssueId = issueId,
            Issue = new Issue
            {
                Id = issueId,
                ProjectId = ProjectId,
                Title = "Issue",
                AuthorId = 99,
                Author = new Modules.Auth.Models.ApplicationUser { Id = 99, UserName = "author" }
            }
        };

    [Fact]
    public async Task Add_CreatesCard_WhenValid()
    {
        var column = MakeColumn(cards: []);
        var board = MakeBoard(columns: [column]);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);
        _cardsRepo.Setup(r => r.Add(It.IsAny<BoardCard>()))
            .Callback<BoardCard>(c => c.Id = CardId);
        _cardsRepo.Setup(r => r.LoadIssueAsync(It.IsAny<BoardCard>()))
            .Callback<BoardCard>(c => c.Issue = new Issue
            {
                Id = c.IssueId,
                ProjectId = ProjectId,
                Title = "Issue",
                Status = Status.Open,
                AuthorId = 99,
                Author = new Modules.Auth.Models.ApplicationUser { Id = 99, UserName = "author" }
            })
            .Returns(Task.CompletedTask);

        var result = await _service.Add(ProjectId, new CreateCardRequest
        {
            IssueId = IssueId,
            ColumnId = ColumnId
        });

        Assert.Equal(CardId, result.Id);
        Assert.Equal(1, result.Position);
        _cardsRepo.Verify(r => r.Add(It.IsAny<BoardCard>()), Times.Once);
    }

    [Fact]
    public async Task Add_Throws_WhenIssueAlreadyOnBoard()
    {
        var existingCard = MakeCard(id: 99, issueId: IssueId);
        var column = MakeColumn(cards: [existingCard]);
        var board = MakeBoard(columns: [column]);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.Add(ProjectId, new CreateCardRequest
            {
                IssueId = IssueId,
                ColumnId = ColumnId
            }));

    }

    [Fact]
    public async Task Add_Throws_WhenColumnNotFound()
    {
        var board = MakeBoard(columns: []);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Add(ProjectId, new CreateCardRequest
            {
                IssueId = IssueId,
                ColumnId = ColumnId
            }));
    }

    [Fact]
    public async Task Add_Throws_WhenNotProjectLead()
    {
        var board = MakeBoard(columns: []);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);
        _authGuard.Setup(g => g.Authorize(It.IsAny<Board>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Add(ProjectId, new CreateCardRequest
            {
                IssueId = IssueId,
                ColumnId = ColumnId
            }));
    }

    [Fact]
    public async Task Delete_RemovesCard()
    {
        var project = new Project
        {
            Id = ProjectId,
            OwnerId = 1,
            Name = "P",
            Description = null,
            Owner = new Modules.Auth.Models.ApplicationUser { Id = 1, UserName = "u" }
        };
        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);
        var card = MakeCard();
        _cardsRepo.Setup(r => r.GetByIdAsync(CardId)).ReturnsAsync(card);

        await _service.Delete(ProjectId, CardId);

        _cardsRepo.Verify(r => r.Remove(card), Times.Once);
    }

    [Fact]
    public async Task Delete_Throws_WhenNotProjectLead()
    {
        var project = new Project
        {
            Id = ProjectId,
            OwnerId = 1,
            Name = "P",
            Description = null,
            Owner = new Modules.Auth.Models.ApplicationUser { Id = 1, UserName = "u" }
        };
        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);
        _authGuard.Setup(g => g.Authorize(It.IsAny<Project>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Delete(ProjectId, CardId));
    }

    [Fact]
    public async Task Move_MovesCardToTargetColumn()
    {
        var card = MakeCard(id: CardId, columnId: ColumnId, position: 1);
        var sourceCol = MakeColumn(id: ColumnId, cards: [card]);
        var targetCol = MakeColumn(id: 21, cards: []);
        var board = MakeBoard(columns: [sourceCol, targetCol]);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);
        _cardsRepo.Setup(r => r.LoadIssueAsync(It.IsAny<BoardCard>()))
            .Callback<BoardCard>(c => c.Issue = new Issue
            {
                Id = c.IssueId,
                ProjectId = ProjectId,
                Title = "Issue",
                Status = Status.Open,
                AuthorId = 99,
                Author = new Modules.Auth.Models.ApplicationUser { Id = 99, UserName = "author" }
            })
            .Returns(Task.CompletedTask);

        var result = await _service.Move(ProjectId, CardId, new MoveCardRequest
        {
            ColumnId = 21,
            Position = 0
        });

        Assert.Equal(21, card.ColumnId);
        Assert.Equal(1, card.Position);
    }

    [Fact]
    public async Task Move_Throws_WhenCardNotFound()
    {
        var board = MakeBoard(columns: [MakeColumn()]);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Move(ProjectId, 999, new MoveCardRequest
            {
                ColumnId = ColumnId,
                Position = 0
            }));
    }

    [Fact]
    public async Task Move_Throws_WhenTargetColumnNotFound()
    {
        var card = MakeCard();
        var col = MakeColumn(cards: [card]);
        var board = MakeBoard(columns: [col]);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Move(ProjectId, CardId, new MoveCardRequest
            {
                ColumnId = 999,
                Position = 0
            }));
    }

    [Fact]
    public async Task Reorder_SetsFinalPositions()
    {
        var card1 = MakeCard(id: 1, position: 1);
        var card2 = MakeCard(id: 2, position: 2);
        var card3 = MakeCard(id: 3, position: 3);
        var col = MakeColumn(cards: [card1, card2, card3]);
        var board = MakeBoard(columns: [col]);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);

        await _service.Reorder(ProjectId, ColumnId, new ReorderCardsRequest { CardIds = [3, 1, 2] });

        Assert.Equal(1, card3.Position);
        Assert.Equal(2, card1.Position);
        Assert.Equal(3, card2.Position);
    }

    [Fact]
    public async Task Reorder_Throws_WhenColumnNotFound()
    {
        var board = MakeBoard(columns: []);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Reorder(ProjectId, ColumnId, new ReorderCardsRequest { CardIds = [] }));
    }

    [Fact]
    public async Task Reorder_Throws_WhenNotProjectLead()
    {
        var board = MakeBoard(columns: []);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);
        _authGuard.Setup(g => g.Authorize(It.IsAny<Board>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Reorder(ProjectId, ColumnId, new ReorderCardsRequest { CardIds = [] }));
    }
}
