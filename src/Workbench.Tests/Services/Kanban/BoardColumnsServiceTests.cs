using Microsoft.AspNetCore.Authorization;
using Moq;
using Workbench.Common.Enums;
using Workbench.Common.Exceptions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Kanban.Dtos.Requests;
using Workbench.Modules.Kanban.Models;
using Workbench.Modules.Kanban.Services.Implementations;
using Workbench.Tests.Helpers;

namespace Workbench.Tests.Services.Kanban;

public class BoardColumnsServiceTests : IDisposable
{
    private const int ProjectId = 1;
    private const int BoardId = 10;
    private const int ColumnId = 20;

    private readonly Data.AppDbContext _db;
    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly BoardColumnsService _service;

    public BoardColumnsServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _authGuard = new Mock<IAuthorizationGuard>();
        _service = new BoardColumnsService(_db, _authGuard.Object);
    }

    public void Dispose() => _db.Dispose();

    private async Task SeedBoard(List<BoardColumn>? columns = null)
    {
        var owner = new Modules.Auth.Models.ApplicationUser { Id = 1, UserName = "owner" };
        _db.Users.Add(owner);
        _db.Projects.Add(new Modules.Projects.Models.Project
        {
            Id = ProjectId,
            OwnerId = 1,
            Name = "P",
            Description = null,
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

    [Fact]
    public async Task Add_CreatesColumn_WithCorrectPosition()
    {
        var col1 = new BoardColumn { Id = 1, Name = "C1", Description = null, Position = 1, Color = Color.Blue, BoardId = BoardId, Cards = [] };
        var col2 = new BoardColumn { Id = 2, Name = "C2", Description = null, Position = 2, Color = Color.Blue, BoardId = BoardId, Cards = [] };
        await SeedBoard(columns: [col1, col2]);

        var result = await _service.Add(ProjectId, new CreateColumnRequest
        {
            Name = "New Col",
            Description = null,
            Color = Color.Red
        });

        Assert.Equal(3, result.Position);
    }

    [Fact]
    public async Task Add_CreatesColumnAtPosition1_WhenNoExistingColumns()
    {
        await SeedBoard(columns: []);

        var result = await _service.Add(ProjectId, new CreateColumnRequest
        {
            Name = "First",
            Description = null,
            Color = Color.Blue
        });

        Assert.Equal(1, result.Position);
    }

    [Fact]
    public async Task Add_Throws_WhenNotProjectLead()
    {
        await SeedBoard();
        _authGuard.Setup(g => g.Authorize(It.IsAny<Board>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Add(ProjectId, new CreateColumnRequest
            {
                Name = "X",
                Description = null,
                Color = Color.Blue
            }));
    }

    [Fact]
    public async Task Update_UpdatesColumnFields()
    {
        var column = new BoardColumn
        {
            Id = ColumnId,
            Name = "Col",
            Description = null,
            Position = 1,
            Color = Color.Blue,
            BoardId = BoardId,
            Cards = [],
        };
        await SeedBoard(columns: [column]);

        var result = await _service.Update(ProjectId, ColumnId, new UpdateColumnRequest
        {
            Name = "Updated",
            Description = "New",
            Color = Color.Red
        });

        Assert.Equal("Updated", result.Name);
        Assert.Equal("New", result.Description);
        Assert.Equal(Color.Red, result.Color);
    }

    [Fact]
    public async Task Update_Throws_WhenColumnNotFound()
    {
        await SeedBoard(columns: []);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Update(ProjectId, ColumnId, new UpdateColumnRequest
            {
                Name = "X",
                Description = null,
                Color = Color.Blue
            }));
    }

    [Fact]
    public async Task Delete_RemovesColumn()
    {
        var column = new BoardColumn
        {
            Id = ColumnId,
            Name = "Col",
            Description = null,
            Position = 1,
            Color = Color.Blue,
            BoardId = BoardId,
            Cards = [],
        };
        await SeedBoard(columns: [column]);

        await _service.Delete(ProjectId, ColumnId);

        Assert.Null(_db.BoardColumns.Find(ColumnId));
    }

    [Fact]
    public async Task Delete_Throws_WhenColumnNotFound()
    {
        await SeedBoard(columns: []);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Delete(ProjectId, ColumnId));
    }

    [Fact]
    public async Task Reorder_SetsFinalPositions()
    {
        var col1 = new BoardColumn { Id = 1, Name = "C1", Description = null, Position = 1, Color = Color.Blue, BoardId = BoardId, Cards = [] };
        var col2 = new BoardColumn { Id = 2, Name = "C2", Description = null, Position = 2, Color = Color.Blue, BoardId = BoardId, Cards = [] };
        var col3 = new BoardColumn { Id = 3, Name = "C3", Description = null, Position = 3, Color = Color.Blue, BoardId = BoardId, Cards = [] };
        await SeedBoard(columns: [col1, col2, col3]);

        await _service.Reorder(ProjectId, new MoveColumnRequest { ColumnIds = [3, 1, 2] });

        Assert.Equal(1, _db.BoardColumns.Find(3)!.Position);
        Assert.Equal(2, _db.BoardColumns.Find(1)!.Position);
        Assert.Equal(3, _db.BoardColumns.Find(2)!.Position);
    }

    [Fact]
    public async Task Reorder_Throws_WhenNotProjectLead()
    {
        await SeedBoard();
        _authGuard.Setup(g => g.Authorize(It.IsAny<Board>(), It.IsAny<IAuthorizationRequirement>()))
            .ThrowsAsync(new ForbiddenException("Not lead"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.Reorder(ProjectId, new MoveColumnRequest { ColumnIds = [] }));
    }
}
