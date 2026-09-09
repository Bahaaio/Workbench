using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Kanban.Dtos;
using Workbench.Modules.Kanban.Models;
using Workbench.Modules.Kanban.Services.Implementations;
using Workbench.Modules.Projects.Enums;
using Workbench.Tests.Helpers;

namespace Workbench.Tests.Services.Kanban;

public class BoardsServiceTests : IDisposable
{
    private const int ProjectId = 1;

    private readonly Data.AppDbContext _db;
    private readonly BoardsService _service;

    public BoardsServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _service = new BoardsService(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task SeedProject()
    {
        var owner = new Modules.Auth.Models.ApplicationUser { Id = 1, UserName = "owner" };
        _db.Users.Add(owner);
        _db.Projects.Add(new Modules.Projects.Models.Project
        {
            Id = ProjectId,
            OwnerId = 1,
            Name = "P",
            Description = null,
            Visibility = ProjectVisibility.Public,
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Get_Throws_WhenProjectNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.Get(ProjectId));
    }

    [Fact]
    public async Task CreateEmpty_CreatesBoardWithName()
    {
        await SeedProject();

        await _service.CreateEmpty(ProjectId);

        var board = _db.Boards.Single(b => b.ProjectId == ProjectId);
        Assert.Equal("Board", board.Name);
    }
}
