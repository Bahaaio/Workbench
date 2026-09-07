using Moq;
using Workbench.Common.Exceptions;
using Workbench.Modules.Auth.Models;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Repositories;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Memberships.Models;
using Workbench.Modules.Projects.Memberships.Repositories;
using Workbench.Modules.Projects.Memberships.Services.Implementations;
using Workbench.Modules.Projects.Models;
using Workbench.Modules.Projects.Repositories;

namespace Workbench.Tests.Services.Memberships;

public class ProjectMembershipsServiceTests
{
    private const int CurrentUserId = 10;
    private const int OtherUserId = 20;
    private const int OwnerUserId = 30;
    private const int ProjectId = 1;

    private readonly Mock<IAuthorizationGuard> _authGuard;
    private readonly Mock<IProjectMembershipsRepository> _membershipsRepo;
    private readonly Mock<IProjectsRepository> _projectsRepo;
    private readonly Mock<IIssuesRepository> _issuesRepo;
    private readonly ProjectMembershipsService _service;

    public ProjectMembershipsServiceTests()
    {
        var userMock = new Mock<ICurrentUser>();
        userMock.Setup(u => u.Id).Returns(CurrentUserId);

        _authGuard = new Mock<IAuthorizationGuard>();
        _membershipsRepo = new Mock<IProjectMembershipsRepository>();
        _projectsRepo = new Mock<IProjectsRepository>();
        _issuesRepo = new Mock<IIssuesRepository>();

        _service = new ProjectMembershipsService(
            _membershipsRepo.Object,
            userMock.Object,
            _projectsRepo.Object,
            _authGuard.Object,
            _issuesRepo.Object);
    }

    private static Project MakeProject(int ownerId = OwnerUserId) =>
        new() { Id = ProjectId, OwnerId = ownerId, Name = "Test", Description = null, CreatedAt = DateTime.UtcNow };

    private static ProjectMembership MakeMembership(int userId = OtherUserId, ProjectMemberRole role = ProjectMemberRole.Member) =>
        new() { ProjectId = ProjectId, UserId = userId, Role = role, User = new ApplicationUser { Id = userId, UserName = $"user{userId}" } };

    [Fact]
    public async Task AddMember_CreatesMembershipAndSaves()
    {
        ProjectMembership? captured = null;
        _membershipsRepo
            .Setup(r => r.Add(It.IsAny<ProjectMembership>()))
            .Callback<ProjectMembership>(m => captured = m);

        await _service.AddMember(ProjectId, OtherUserId, ProjectMemberRole.Member);

        Assert.NotNull(captured);
        Assert.Equal(ProjectId, captured.ProjectId);
        Assert.Equal(OtherUserId, captured.UserId);
        Assert.Equal(ProjectMemberRole.Member, captured.Role);
    }

    [Fact]
    public async Task UpdateRole_ChangesRole_WhenAuthorized()
    {
        var project = MakeProject();
        var membership = MakeMembership(OtherUserId);

        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);
        _membershipsRepo.Setup(r => r.GetByProjectIdAndUsernameAsync(ProjectId, "user20")).ReturnsAsync(membership);

        await _service.UpdateRole(ProjectId, "user20", ProjectMemberRole.Lead);

        Assert.Equal(ProjectMemberRole.Lead, membership.Role);
    }

    [Fact]
    public async Task UpdateRole_Throws_WhenChangingOwnRole()
    {
        var project = MakeProject();
        var membership = MakeMembership(CurrentUserId);

        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);
        _membershipsRepo.Setup(r => r.GetByProjectIdAndUsernameAsync(ProjectId, "user10")).ReturnsAsync(membership);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.UpdateRole(ProjectId, "user10", ProjectMemberRole.Lead));

    }

    [Fact]
    public async Task UpdateRole_Throws_WhenChangingOwnerRole()
    {
        var project = MakeProject();
        var membership = MakeMembership(OwnerUserId);

        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);
        _membershipsRepo.Setup(r => r.GetByProjectIdAndUsernameAsync(ProjectId, "user30")).ReturnsAsync(membership);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.UpdateRole(ProjectId, "user30", ProjectMemberRole.Member));

    }

    [Fact]
    public async Task RemoveMember_RemovesAndUnassignsIssues()
    {
        var project = MakeProject();
        var membership = MakeMembership(OtherUserId);

        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);
        _membershipsRepo.Setup(r => r.GetByProjectIdAndUsernameAsync(ProjectId, "user20")).ReturnsAsync(membership);

        await _service.RemoveMember(ProjectId, "user20");

        _issuesRepo.Verify(r => r.UnassignFromAllAsync(ProjectId, OtherUserId), Times.Once);
        _membershipsRepo.Verify(r => r.Remove(membership), Times.Once);
    }

    [Fact]
    public async Task RemoveMember_Throws_WhenRemovingSelf()
    {
        var project = MakeProject();
        var membership = MakeMembership(CurrentUserId);

        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);
        _membershipsRepo.Setup(r => r.GetByProjectIdAndUsernameAsync(ProjectId, "user10")).ReturnsAsync(membership);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.RemoveMember(ProjectId, "user10"));

        _membershipsRepo.Verify(r => r.Remove(It.IsAny<ProjectMembership>()), Times.Never);
    }

    [Fact]
    public async Task RemoveMember_Throws_WhenRemovingOwner()
    {
        var project = MakeProject();
        var membership = MakeMembership(OwnerUserId);

        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);
        _membershipsRepo.Setup(r => r.GetByProjectIdAndUsernameAsync(ProjectId, "user30")).ReturnsAsync(membership);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.RemoveMember(ProjectId, "user30"));

        _membershipsRepo.Verify(r => r.Remove(It.IsAny<ProjectMembership>()), Times.Never);
    }

    [Fact]
    public async Task LeaveProject_RemovesMembership_WhenMember()
    {
        var project = MakeProject();
        var membership = MakeMembership(CurrentUserId);

        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);
        _membershipsRepo.Setup(r => r.FindMembershipByProjectIdAndUserId(ProjectId, CurrentUserId))
            .ReturnsAsync(membership);

        await _service.LeaveProject(ProjectId);

        _issuesRepo.Verify(r => r.UnassignFromAllAsync(ProjectId, CurrentUserId), Times.Once);
        _membershipsRepo.Verify(r => r.Remove(membership), Times.Once);
    }

    [Fact]
    public async Task LeaveProject_Throws_WhenNotMember()
    {
        var project = MakeProject();

        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);
        _membershipsRepo.Setup(r => r.FindMembershipByProjectIdAndUserId(ProjectId, CurrentUserId))
            .ReturnsAsync((ProjectMembership?)null);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.LeaveProject(ProjectId));

        _membershipsRepo.Verify(r => r.Remove(It.IsAny<ProjectMembership>()), Times.Never);
    }

    [Fact]
    public async Task LeaveProject_Throws_WhenOwner()
    {
        var project = MakeProject(CurrentUserId);
        var membership = MakeMembership(CurrentUserId);

        _projectsRepo.Setup(r => r.GetByIdAsync(ProjectId)).ReturnsAsync(project);
        _membershipsRepo.Setup(r => r.FindMembershipByProjectIdAndUserId(ProjectId, CurrentUserId))
            .ReturnsAsync(membership);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.LeaveProject(ProjectId));

        _membershipsRepo.Verify(r => r.Remove(It.IsAny<ProjectMembership>()), Times.Never);
    }

    [Fact]
    public async Task IsMember_ReturnsTrue_WhenMembershipExists()
    {
        _membershipsRepo.Setup(r => r.FindMembershipByProjectIdAndUserId(ProjectId, OtherUserId))
            .ReturnsAsync(MakeMembership(OtherUserId));

        var result = await _service.IsMember(ProjectId, OtherUserId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsMember_ReturnsFalse_WhenNoMembership()
    {
        _membershipsRepo.Setup(r => r.FindMembershipByProjectIdAndUserId(ProjectId, OtherUserId))
            .ReturnsAsync((ProjectMembership?)null);

        var result = await _service.IsMember(ProjectId, OtherUserId);

        Assert.False(result);
    }
}
