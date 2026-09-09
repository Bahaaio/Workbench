using Microsoft.EntityFrameworkCore;
using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Auth.Models;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Memberships.Services.Implementations;
using Workbench.Tests.Helpers;

namespace Workbench.Tests.Services.Memberships;

public class ProjectMembershipsServiceTests : IDisposable
{
    private const int CurrentUserId = 10;
    private const int OtherUserId = 20;
    private const int OwnerUserId = 30;
    private const int ProjectId = 1;

    private readonly Data.AppDbContext _db;
    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly ProjectMembershipsService _service;

    public ProjectMembershipsServiceTests()
    {
        _db = TestDbContextFactory.Create();

        var userMock = new Mock<ICurrentUser>();
        userMock.Setup(u => u.Id).Returns(CurrentUserId);

        _authGuard = new Mock<IAuthorizationGuard>();

        _service = new ProjectMembershipsService(
            _db,
            userMock.Object,
            _authGuard.Object);
    }

    public void Dispose() => _db.Dispose();

    private async Task SeedProject(int ownerId = OwnerUserId)
    {
        var users = new List<ApplicationUser>
        {
            new() { Id = ownerId, UserName = $"user{ownerId}" },
            new() { Id = CurrentUserId, UserName = $"user{CurrentUserId}" },
            new() { Id = OtherUserId, UserName = $"user{OtherUserId}" },
        };
        var distinctUsers = users.GroupBy(u => u.Id).Select(g => g.First()).ToList();
        _db.Users.AddRange(distinctUsers);
        _db.Projects.Add(new Modules.Projects.Models.Project
        {
            Id = ProjectId,
            OwnerId = ownerId,
            Name = "Test",
            Description = null,
            Visibility = Modules.Projects.Enums.ProjectVisibility.Public,
        });
        await _db.SaveChangesAsync();
    }

    private async Task SeedMembership(int userId = OtherUserId, ProjectMemberRole role = ProjectMemberRole.Member)
    {
        var trackedIds = _db.ChangeTracker.Entries<ApplicationUser>().Select(e => e.Entity.Id).ToHashSet();
        if (!trackedIds.Contains(userId))
        {
            _db.Users.Add(new ApplicationUser { Id = userId, UserName = $"user{userId}" });
        }
        _db.ProjectMemberships.Add(new Modules.Projects.Memberships.Models.ProjectMembership
        {
            ProjectId = ProjectId,
            UserId = userId,
            Role = role,
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task AddMember_CreatesMembershipAndSaves()
    {
        await SeedProject();

        await _service.AddMember(ProjectId, OtherUserId, ProjectMemberRole.Member);

        var membership = _db.ProjectMemberships.Single(m =>
            m.ProjectId == ProjectId && m.UserId == OtherUserId);
        Assert.Equal(ProjectMemberRole.Member, membership.Role);
    }

    [Fact]
    public async Task UpdateRole_ChangesRole_WhenAuthorized()
    {
        await SeedProject();
        await SeedMembership(OtherUserId);

        await _service.UpdateRole(ProjectId, "user20", ProjectMemberRole.Lead);

        var membership = _db.ProjectMemberships.Single(m =>
            m.ProjectId == ProjectId && m.UserId == OtherUserId);
        Assert.Equal(ProjectMemberRole.Lead, membership.Role);
    }

    [Fact]
    public async Task UpdateRole_Throws_WhenChangingOwnRole()
    {
        await SeedProject();
        await SeedMembership(CurrentUserId);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.UpdateRole(ProjectId, "user10", ProjectMemberRole.Lead));
    }

    [Fact]
    public async Task UpdateRole_Throws_WhenChangingOwnerRole()
    {
        await SeedProject(ownerId: OwnerUserId);
        await SeedMembership(OwnerUserId);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.UpdateRole(ProjectId, $"user{OwnerUserId}", ProjectMemberRole.Member));
    }

    [Fact]
    public async Task RemoveMember_RemovesAndUnassignsIssues()
    {
        await SeedProject();
        await SeedMembership(OtherUserId);
        var issue = new Issue
        {
            Id = 100,
            ProjectId = ProjectId,
            Title = "Issue",
            AuthorId = 99,
            AssignedToId = OtherUserId,
            Status = Status.Open,
        };
        var author = new ApplicationUser { Id = 99, UserName = "author" };
        _db.Users.Add(author);
        _db.Issues.Add(issue);
        await _db.SaveChangesAsync();
        _db.Entry(issue).State = EntityState.Detached;

        await _service.RemoveMember(ProjectId, "user20");

        Assert.Null(_db.ProjectMemberships.SingleOrDefault(m =>
            m.ProjectId == ProjectId && m.UserId == OtherUserId));
        var updatedIssue = await _db.Issues.FindAsync(100);
        Assert.Null(updatedIssue!.AssignedToId);
    }

    [Fact]
    public async Task RemoveMember_Throws_WhenRemovingSelf()
    {
        await SeedProject();
        await SeedMembership(CurrentUserId);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.RemoveMember(ProjectId, "user10"));
    }

    [Fact]
    public async Task RemoveMember_Throws_WhenRemovingOwner()
    {
        await SeedProject(ownerId: OwnerUserId);
        await SeedMembership(OwnerUserId);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.RemoveMember(ProjectId, $"user{OwnerUserId}"));
    }

    [Fact]
    public async Task LeaveProject_RemovesMembership_WhenMember()
    {
        await SeedProject();
        await SeedMembership(CurrentUserId);

        await _service.LeaveProject(ProjectId);

        Assert.Null(_db.ProjectMemberships.SingleOrDefault(m =>
            m.ProjectId == ProjectId && m.UserId == CurrentUserId));
    }

    [Fact]
    public async Task LeaveProject_Throws_WhenNotMember()
    {
        await SeedProject();

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.LeaveProject(ProjectId));
    }

    [Fact]
    public async Task LeaveProject_Throws_WhenOwner()
    {
        await SeedProject(ownerId: CurrentUserId);
        await SeedMembership(CurrentUserId);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.LeaveProject(ProjectId));
    }

    [Fact]
    public async Task IsMember_ReturnsTrue_WhenMembershipExists()
    {
        await SeedProject();
        await SeedMembership(OtherUserId);

        var result = await _service.IsMember(ProjectId, OtherUserId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsMember_ReturnsFalse_WhenNoMembership()
    {
        await SeedProject();

        var result = await _service.IsMember(ProjectId, OtherUserId);

        Assert.False(result);
    }
}
