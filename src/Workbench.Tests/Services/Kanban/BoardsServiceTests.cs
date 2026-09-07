using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Kanban.Dtos;
using Workbench.Modules.Kanban.Models;
using Workbench.Modules.Kanban.Repositories;
using Workbench.Modules.Kanban.Services.Implementations;
using Workbench.Modules.Projects.Repositories;

namespace Workbench.Tests.Services.Kanban;

public class BoardsServiceTests
{
    private const int ProjectId = 1;
    private const int BoardId = 10;

    private readonly Mock<IBoardsRepository> _boardsRepo;
    private readonly Mock<IProjectsRepository> _projectsRepo;
    private readonly BoardsService _service;

    public BoardsServiceTests()
    {
        _boardsRepo = new Mock<IBoardsRepository>();
        _projectsRepo = new Mock<IProjectsRepository>();

        _service = new BoardsService(
            _boardsRepo.Object,
            _projectsRepo.Object);
    }

    [Fact]
    public async Task Get_ReturnsBoard_WhenProjectExists()
    {
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId)).Returns(Task.CompletedTask);
        _boardsRepo.Setup(r => r.GetByProjectId(ProjectId))
            .ReturnsAsync(new BoardDto
            {
                Id = BoardId,
                Name = "Board",
                Columns = []
            });

        var result = await _service.Get(ProjectId);

        Assert.Equal(BoardId, result.Id);
        Assert.Equal("Board", result.Name);
    }

    [Fact]
    public async Task Get_Throws_WhenProjectNotFound()
    {
        _projectsRepo.Setup(r => r.ExistsOrThrowAsync(ProjectId))
            .ThrowsAsync(new NotFoundException("Not found"));

        await Assert.ThrowsAsync<NotFoundException>(() => _service.Get(ProjectId));
    }

    [Fact]
    public async Task CreateEmpty_CreatesBoardWithName()
    {
        _boardsRepo.Setup(r => r.Add(It.IsAny<Board>()))
            .Callback<Board>(b => b.Id = BoardId);

        await _service.CreateEmpty(ProjectId);

        _boardsRepo.Verify(r => r.Add(It.Is<Board>(b =>
            b.Name == "Board" && b.ProjectId == ProjectId)), Times.Once);
    }
}
