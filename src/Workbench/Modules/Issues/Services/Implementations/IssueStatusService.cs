using Microsoft.EntityFrameworkCore;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Dtos;
using Workbench.Modules.Issues.Dtos.Requests;
using Workbench.Modules.Issues.Mappers;
using Workbench.Modules.Issues.Models;

namespace Workbench.Modules.Issues.Services.Implementations;

public class IssueStatusService : IIssueStatusService
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationGuard _authGuard;
    private readonly ILogger<IssueStatusService> _logger;
    private readonly ICurrentUser _user;

    public IssueStatusService(AppDbContext dbContext,
        ICurrentUser user, IAuthorizationGuard authGuard, ILogger<IssueStatusService> logger)
    {
        _db = dbContext;
        _user = user;
        _authGuard = authGuard;
        _logger = logger;
    }

    public async Task UpdateStatus(int issueId, UpdateIssueStatusRequest request)
    {
        var issue = await _db.Issues.FindOrThrowAsync(issueId);
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
        _db.IssueStatusChanges.Add(statusChange);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {userId} updated issue {issueId} status from {from} to {to}",
            _user.Id, issue.Id, statusChange.FromStatus, statusChange.ToStatus);
    }

    public async Task<List<StatusChangeDto>> GetStatusHistory(int issueId)
    {
        await _db.Issues.ExistsOrThrowAsync(issueId);
        return await _db.IssueStatusChanges
            .AsNoTracking()
            .Where(s => s.IssueId == issueId)
            .OrderBy(s => s.ChangedAt)
            .Select(StatusChangeMapper.ToDtoExpression)
            .ToListAsync();
    }
}