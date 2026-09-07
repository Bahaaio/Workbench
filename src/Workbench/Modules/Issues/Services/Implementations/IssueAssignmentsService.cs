using Workbench.Common.Exceptions;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Dtos;
using Workbench.Modules.Issues.Dtos.Requests;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Issues.Repositories;
using Workbench.Modules.Projects.Memberships.Services;

namespace Workbench.Modules.Issues.Services.Implementations;

public class IssueAssignmentsService : IIssueAssignmentsService
{
    private readonly IAuthorizationGuard _authGuard;
    private readonly IIssuesRepository _issuesRepository;
    private readonly IProjectMembershipsService _membershipsService;
    private readonly ICurrentUser _user;

    public IssueAssignmentsService(IIssuesRepository issuesRepository,
        ICurrentUser user, IAuthorizationGuard authGuard,
        IProjectMembershipsService membershipsService)
    {
        _issuesRepository = issuesRepository;
        _user = user;
        _authGuard = authGuard;
        _membershipsService = membershipsService;
    }

    public async Task AssignCurrentUser(int issueId)
    {
        var issue = await _issuesRepository.GetByIdAsync(issueId);
        await _authGuard.AuthorizeProjectMember(issue);

        ValidateClosedIssue(issue);

        if (issue.AssignedToId is not null)
            throw new ConflictException("Issue is already assigned to a user");

        issue.AssignedToId = _user.Id;
        await _issuesRepository.SaveChangesAsync();
    }

    public async Task UnassignCurrentUser(int issueId)
    {
        var issue = await _issuesRepository.GetByIdAsync(issueId);
        await _authGuard.AuthorizeAssignedOrProjectLead(issue);

        ValidateClosedIssue(issue);

        if (issue.AssignedToId != _user.Id)
            throw new ForbiddenException("Issue is not assigned to the current user");

        issue.AssignedToId = null;
        await _issuesRepository.SaveChangesAsync();
    }

    public async Task AssignUser(int issueId, string userName)
    {
        var issue = await _issuesRepository.GetByIdAsync(issueId);
        await _authGuard.AuthorizeProjectLead(issue);

        ValidateClosedIssue(issue);

        var membership = await _membershipsService.GetProjectMembership(issue.ProjectId, userName);
        issue.AssignedToId = membership.UserId;

        await _issuesRepository.SaveChangesAsync();
    }

    public async Task UnassignUser(int issueId)
    {
        var issue = await _issuesRepository.GetByIdAsync(issueId);
        await _authGuard.AuthorizeProjectLead(issue);

        ValidateClosedIssue(issue);

        issue.AssignedToId = null;
        await _issuesRepository.SaveChangesAsync();
    }

    public Task<List<IssueDto>> GetCurrentUserAssignedIssues(IssueQuery issueQuery) =>
        _issuesRepository.GetAllAssignedToUserAsync(_user.Id, issueQuery);

    private static void ValidateClosedIssue(Issue issue)
    {
        if (issue.Status == Status.Closed)
            throw new ConflictException("Issue is already closed");
    }
}