using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Dtos;
using Workbench.Modules.Issues.Dtos.Requests;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Issues.Repositories;

namespace Workbench.Modules.Issues.Services.Implementations;

public class IssueStatusService : IIssueStatusService
{
    private readonly IAuthorizationGuard _authGuard;
    private readonly IIssuesRepository _issuesRepository;
    private readonly ILogger<IssueStatusService> _logger;
    private readonly IIssueStatusChangeRepository _statusChangeRepository;
    private readonly ICurrentUser _user;

    public IssueStatusService(IIssuesRepository issuesRepository,
        IIssueStatusChangeRepository statusChangeRepository,
        ICurrentUser user, IAuthorizationGuard authGuard, ILogger<IssueStatusService> logger)
    {
        _issuesRepository = issuesRepository;
        _statusChangeRepository = statusChangeRepository;
        _user = user;
        _authGuard = authGuard;
        _logger = logger;
    }

    public async Task UpdateStatus(int issueId, UpdateIssueStatusRequest request)
    {
        var issue = await _issuesRepository.GetByIdAsync(issueId);
        await _authGuard.AuthorizeProjectMember(issue);

        if (issue.Status == request.Status)
            return;

        var statusChange = new IssueStatusChange
        {
            IssueId = issueId,
            FromStatus = issue.Status,
            ToStatus = request.Status,
            ChangedByUserId = _user.Id
        };

        issue.Status = request.Status;
        _statusChangeRepository.Add(statusChange);
        await _statusChangeRepository.SaveChangesAsync();

        _logger.LogInformation("User {userId} updated issue {issueId} status from {from} to {to}",
            _user.Id, issue.Id, statusChange.FromStatus, statusChange.ToStatus);
    }

    public async Task<List<StatusChangeDto>> GetStatusHistory(int issueId)
    {
        await _issuesRepository.ExistsOrThrowAsync(issueId);
        return await _statusChangeRepository.GetHistoryAsync(issueId);
    }
}