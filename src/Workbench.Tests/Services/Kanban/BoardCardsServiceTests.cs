using Microsoft.AspNetCore.Authorization;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Common.Enums;
using Workbench.Modules.Auth.Models;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Kanban.Dtos.Requests;
using Workbench.Modules.Kanban.Models;
using Workbench.Modules.Kanban.Services.Implementations;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Models;
using Workbench.Tests.Helpers;

namespace Workbench.Tests.Services.Kanban;

public class BoardCardsServiceTests : IDisposable
{
    private const int ProjectId = 1;
    private const int BoardId = 10;
    private const int ColumnId = 20;
    private const int IssueId = 100;

    private readonly Data.AppDbContext _db;
    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly BoardCardsService _service;

    public BoardCardsServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _authGuard = new Mock<IAuthorizationGuard>();
        _service = new BoardCardsService(_db, _authGuard.Object);
    }

    public void Dispose() => _db.Dispose();

    private async Task SeedBoard(List<BoardColumn>? columns = null)
    {
        var owner = new ApplicationUser { Id = 1, UserName = "owner" };
        _db.Users.Add(owner);
        _db.Projects.Add(new Project
        {
            Id = ProjectId,
            OwnerId = 1,
            Name = "P",
            Description = null,
            Visibility = ProjectVisibility.Public,
        });

        var board = new Board
        {
            Id = BoardId,
            Name = "Board",
            ProjectId = ProjectId,
            Columns = columns ?? [],
        };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync();
    }

    private async Task SeedIssue(int issueId = IssueId)
    {
        var author = new ApplicationUser { Id = 99, UserName = "author" };
        _db.Users.Add(author);
        _db.Issues.Add(new Issue
        {
            Id = issueId,
            ProjectId = ProjectId,
            Title = "Issue",
            AuthorId = 99,
            Status = Status.Open,
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Add_CreatesCard_WhenValid()
    {
        var column = new BoardColumn
        {
            Id = ColumnId,
            Name = "Col",
            Description = null,
            Position = 1,
            Color = Color.Blue,
            BoardId = BoardId,
            MaxCards = 10,
            Cards = [],
        };
        await SeedBoard(columns: [column]);
        await SeedIssue();

        var result = await _service.Add(ProjectId, new CreateCardRequest
        {
            IssueId = IssueId,
            ColumnId = ColumnId
        });

        Assert.Equal(IssueId, result.IssueId);
        Assert.Equal(1, result.Position);
    }

    [Fact]
    public async Task Add_Throws_WhenIssueAlreadyOnBoard()
    {
        var column = new BoardColumn
        {
            Id = ColumnId,
            Name = "Col",
            Description = null,
            Position = 1,
            Color = Color.Blue,
            BoardId = BoardId,
            MaxCards = 10,
            Cards = [],
        };
        await SeedBoard(columns: [column]);
        await SeedIssue();

        var existingCard = new BoardCard
        {
            Id = 99,
            Position = 1,
            BoardId = BoardId,
            ColumnId = ColumnId,
            IssueId = IssueId,
        };
        _db.BoardCards.Add(existingCard);
        await _db.SaveChangesAsync();

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
        await SeedBoard(columns: []);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Add(ProjectId, new CreateCardRequest
            {
                IssueId = IssueId,
                ColumnId = ColumnId
            }));
    }

    [Fact]
    public async Task Add_Throws_WhenColumnFull()
    {
        var column = new BoardColumn
        {
            Id = ColumnId,
            Name = "Col",
            Description = null,
            Position = 1,
            Color = Color.Blue,
            BoardId = BoardId,
            MaxCards = 1,
            Cards = [],
        };
        await SeedBoard(columns: [column]);
        await SeedIssue();

        var existingCard = new BoardCard
        {
            Id = 99,
            Position = 1,
            BoardId = BoardId,
            ColumnId = ColumnId,
            IssueId = IssueId,
        };
        _db.BoardCards.Add(existingCard);
        await _db.SaveChangesAsync();

        var issue2 = new Issue { Id = 200, ProjectId = ProjectId, Title = "I2", AuthorId = 99, Status = Status.Open };
        _db.Issues.Add(issue2);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.Add(ProjectId, new CreateCardRequest
            {
                IssueId = 200,
                ColumnId = ColumnId
            }));
    }

    [Fact]
    public async Task Move_Throws_WhenTargetColumnFull()
    {
        var sourceColumn = new BoardColumn
        {
            Id = ColumnId,
            Name = "Source",
            Description = null,
            Position = 1,
            Color = Color.Blue,
            BoardId = BoardId,
            MaxCards = 10,
            Cards = [],
        };
        var targetColumn = new BoardColumn
        {
            Id = 30,
            Name = "Target",
            Description = null,
            Position = 2,
            Color = Color.Red,
            BoardId = BoardId,
            MaxCards = 1,
            Cards = [],
        };
        await SeedBoard(columns: [sourceColumn, targetColumn]);
        await SeedIssue();
        var author = new ApplicationUser { Id = 98, UserName = "author2" };
        _db.Users.Add(author);
        _db.Issues.Add(new Issue { Id = 200, ProjectId = ProjectId, Title = "I2", AuthorId = 98, Status = Status.Open });
        await _db.SaveChangesAsync();

        var card = new BoardCard
        {
            Id = 50,
            Position = 1,
            BoardId = BoardId,
            ColumnId = ColumnId,
            IssueId = IssueId,
        };
        var blocker = new BoardCard
        {
            Id = 51,
            Position = 1,
            BoardId = BoardId,
            ColumnId = 30,
            IssueId = 200,
        };
        _db.BoardCards.Add(card);
        _db.BoardCards.Add(blocker);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.Move(ProjectId, card.Id, new MoveCardRequest
            {
                ColumnId = 30,
                Position = 0
            }));
    }

    [Fact]
    public async Task Move_Succeeds_WhenSameColumn()
    {
        var column = new BoardColumn
        {
            Id = ColumnId,
            Name = "Col",
            Description = null,
            Position = 1,
            Color = Color.Blue,
            BoardId = BoardId,
            MaxCards = 10,
            Cards = [],
        };
        await SeedBoard(columns: [column]);
        await SeedIssue();
        var author = new ApplicationUser { Id = 97, UserName = "author3" };
        _db.Users.Add(author);
        _db.Issues.Add(new Issue { Id = 300, ProjectId = ProjectId, Title = "I3", AuthorId = 97, Status = Status.Open });
        await _db.SaveChangesAsync();

        var card1 = new BoardCard { Id = 10, Position = 1, BoardId = BoardId, ColumnId = ColumnId, IssueId = IssueId };
        var card2 = new BoardCard { Id = 11, Position = 2, BoardId = BoardId, ColumnId = ColumnId, IssueId = 300 };
        _db.BoardCards.Add(card1);
        _db.BoardCards.Add(card2);
        await _db.SaveChangesAsync();

        var result = await _service.Move(ProjectId, card1.Id, new MoveCardRequest
        {
            ColumnId = ColumnId,
            Position = 1
        });

        Assert.Equal(2, result.Position);
    }

    [Fact]
    public async Task Delete_RemovesCard()
    {
        var column = new BoardColumn
        {
            Id = ColumnId,
            Name = "Col",
            Description = null,
            Position = 1,
            Color = Color.Blue,
            BoardId = BoardId,
            MaxCards = 10,
            Cards = [],
        };
        await SeedBoard(columns: [column]);
        await SeedIssue();

        var card = new BoardCard
        {
            Id = 30,
            Position = 1,
            BoardId = BoardId,
            ColumnId = ColumnId,
            IssueId = IssueId,
        };
        _db.BoardCards.Add(card);
        await _db.SaveChangesAsync();

        await _service.Delete(ProjectId, card.Id);

        Assert.Null(_db.BoardCards.Find(card.Id));
    }

    [Fact]
    public async Task Delete_Throws_WhenNotProjectLead()
    {
        await SeedBoard();
        _authGuard.Setup(g => g.Authorize(It.IsAny<Board>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Delete(ProjectId, 999));
    }

    [Fact]
    public async Task Reorder_SetsFinalPositions()
    {
        await SeedBoard(columns: []);

        var author = new ApplicationUser { Id = 99, UserName = "author" };
        _db.Users.Add(author);
        _db.Issues.Add(new Issue { Id = 1, ProjectId = ProjectId, Title = "I1", AuthorId = 99, Status = Status.Open });
        _db.Issues.Add(new Issue { Id = 2, ProjectId = ProjectId, Title = "I2", AuthorId = 99, Status = Status.Open });
        _db.Issues.Add(new Issue { Id = 3, ProjectId = ProjectId, Title = "I3", AuthorId = 99, Status = Status.Open });

        var card1 = new BoardCard { Id = 1, Position = 1, BoardId = BoardId, ColumnId = ColumnId, IssueId = 1 };
        var card2 = new BoardCard { Id = 2, Position = 2, BoardId = BoardId, ColumnId = ColumnId, IssueId = 2 };
        var card3 = new BoardCard { Id = 3, Position = 3, BoardId = BoardId, ColumnId = ColumnId, IssueId = 3 };
        var column = new BoardColumn
        {
            Id = ColumnId,
            Name = "Col",
            Description = null,
            Position = 1,
            Color = Color.Blue,
            BoardId = BoardId,
            MaxCards = 10,
            Cards = [card1, card2, card3],
        };
        _db.BoardColumns.Add(column);
        await _db.SaveChangesAsync();

        await _service.Reorder(ProjectId, ColumnId, new ReorderCardsRequest { CardIds = [3, 1, 2] });

        var c3 = _db.BoardCards.Find(3);
        var c1 = _db.BoardCards.Find(1);
        var c2 = _db.BoardCards.Find(2);
        Assert.Equal(1, c3!.Position);
        Assert.Equal(2, c1!.Position);
        Assert.Equal(3, c2!.Position);
    }
}
