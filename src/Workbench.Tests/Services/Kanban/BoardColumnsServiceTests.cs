using Microsoft.AspNetCore.Authorization;
using Moq;
using Workbench.Common.Enums;
using Workbench.Common.Exceptions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Kanban.Dtos.Requests;
using Workbench.Modules.Kanban.Models;
using Workbench.Modules.Kanban.Repositories;
using Workbench.Modules.Kanban.Services.Implementations;
using Workbench.Modules.Projects.Models;
using Workbench.Modules.Projects.Repositories;

namespace Workbench.Tests.Services.Kanban;

public class BoardColumnsServiceTests
{
    private const int ProjectId = 1;
    private const int BoardId = 10;
    private const int ColumnId = 20;

    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly Mock<IBoardsRepository> _boardsRepo;
    private readonly Mock<IBoardColumnsRepository> _columnsRepo;
    private readonly Mock<IProjectsRepository> _projectsRepo;
    private readonly BoardColumnsService _service;

    public BoardColumnsServiceTests()
    {
        _authGuard = new Mock<IAuthorizationGuard>();
        _boardsRepo = new Mock<IBoardsRepository>();
        _columnsRepo = new Mock<IBoardColumnsRepository>();
        _projectsRepo = new Mock<IProjectsRepository>();

        _service = new BoardColumnsService(
            _boardsRepo.Object,
            _columnsRepo.Object,
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

    private static BoardColumn MakeColumn(int id = ColumnId, int position = 1) =>
        new()
        {
            Id = id,
            Name = "Col",
            Description = null,
            Position = position,
            Color = Color.Blue,
            BoardId = BoardId,
            Board = null!,
            Cards = []
        };

    [Fact]
    public async Task Add_CreatesColumn_WithCorrectPosition()
    {
        var board = MakeBoard(columns: [MakeColumn(position: 1), MakeColumn(id: 21, position: 2)]);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);
        _columnsRepo.Setup(r => r.Add(It.IsAny<BoardColumn>()))
            .Callback<BoardColumn>(c => c.Id = ColumnId);

        var result = await _service.Add(ProjectId, new CreateColumnRequest
        {
            Name = "New Col",
            Description = null,
            Color = Color.Red
        });

        Assert.Equal(3, result.Position);
        _columnsRepo.Verify(r => r.Add(It.IsAny<BoardColumn>()), Times.Once);
    }

    [Fact]
    public async Task Add_CreatesColumnAtPosition1_WhenNoExistingColumns()
    {
        var board = MakeBoard(columns: []);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);
        _columnsRepo.Setup(r => r.Add(It.IsAny<BoardColumn>()))
            .Callback<BoardColumn>(c => c.Id = ColumnId);

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
        var board = MakeBoard();
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);
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
        var column = MakeColumn();
        var board = MakeBoard(columns: [column]);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);

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
        var board = MakeBoard(columns: []);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);

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
        var column = MakeColumn();
        var board = MakeBoard(columns: [column]);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);

        await _service.Delete(ProjectId, ColumnId);

        _columnsRepo.Verify(r => r.Remove(column), Times.Once);
    }

    [Fact]
    public async Task Delete_Throws_WhenColumnNotFound()
    {
        var board = MakeBoard(columns: []);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.Delete(ProjectId, ColumnId));

        _columnsRepo.Verify(r => r.Remove(It.IsAny<BoardColumn>()), Times.Never);
    }

    [Fact]
    public async Task Reorder_SetsFinalPositions()
    {
        var col1 = MakeColumn(id: 1, position: 1);
        var col2 = MakeColumn(id: 2, position: 2);
        var col3 = MakeColumn(id: 3, position: 3);
        var board = MakeBoard(columns: [col1, col2, col3]);
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectIdRaw(ProjectId)).ReturnsAsync(board);

        await _service.Reorder(ProjectId, new MoveColumnRequest { ColumnIds = [3, 1, 2] });

        Assert.Equal(1, col3.Position);
        Assert.Equal(2, col1.Position);
        Assert.Equal(3, col2.Position);
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
            () => _service.Reorder(ProjectId, new MoveColumnRequest { ColumnIds = [] }));
    }
}
