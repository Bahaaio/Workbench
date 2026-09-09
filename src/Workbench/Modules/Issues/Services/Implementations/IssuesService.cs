using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Attachments.Services;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Dtos;
using Workbench.Modules.Issues.Dtos.Requests;
using Workbench.Modules.Issues.Extensions;
using Workbench.Modules.Issues.Mappers;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Projects.Enums;
using Workbench.Modules.Projects.Models;

namespace Workbench.Modules.Issues.Services.Implementations;

public class IssuesService : IIssuesService
{
    private readonly AppDbContext _db;
    private readonly IAttachmentsService<Issue> _attachmentsService;
    private readonly IAuthorizationGuard _authGuard;
    private readonly ILogger<IssuesService> _logger;
    private readonly ICurrentUser _user;

    public IssuesService(AppDbContext dbContext,
        ICurrentUser user, IAuthorizationGuard authGuard, ILogger<IssuesService> logger,
        IAttachmentsService<Issue> attachmentsService)
    {
        _db = dbContext;
        _user = user;
        _authGuard = authGuard;
        _logger = logger;
        _attachmentsService = attachmentsService;
    }

    public async Task<IssueDto> GetById(int projectId, int issueId)
    {
        var project = await _db.Projects.FindOrThrowAsync(projectId);
        if (!await IsVisibleToUser(project))
            throw new ForbiddenException("You are not a member of this project");

        return (await _db.Issues.FindOrThrowAsync(issueId)).ToDto();
    }

    public async Task<List<IssueDto>> GetAll(int projectId, IssueQuery issueQuery)
    {
        var project = await _db.Projects.FindOrThrowAsync(projectId);
        if (!await IsVisibleToUser(project))
            return [];

        return await _db.Issues
            .AsNoTracking()
            .Where(i => i.ProjectId == projectId)
            .ApplyFilters(issueQuery)
            .Select(IssueMapper.ToDtoExpression)
            .ToListAsync();
    }

    public async Task<List<IssueDto>> GetCurrentUserIssues(IssueQuery issueQuery)
    {
        var visibleProjectIds = GetVisibleProjectIds();

        return await _db.Issues
            .AsNoTracking()
            .Where(i => i.AuthorId == _user.Id && visibleProjectIds.Contains(i.ProjectId))
            .ApplyFilters(issueQuery)
            .Select(IssueMapper.ToDtoExpression)
            .ToListAsync();
    }

    public async Task<IssueDto> Create(int projectId, CreateIssueRequest request)
    {
        var project = await _db.Projects.FindOrThrowAsync(projectId);
        if (!await IsVisibleToUser(project))
            throw new ForbiddenException("You are not a member of this project");

        var issue = new Issue
        {
            Title = request.Title,
            Description = request.Description,
            AuthorId = _user.Id,
            ProjectId = projectId,
            Project = project
        };

        _db.Issues.Add(issue);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {userId} created issue {issueId}", _user.Id, issue.Id);

        await _db.Entry(issue).Reference(i => i.Author).LoadAsync();
        return issue.ToDto();
    }

    public async Task<IssueDto> Update(int projectId, int issueId, UpdateIssueRequest request)
    {
        await _db.Projects.ExistsOrThrowAsync(projectId);
        var issue = await _db.Issues.FindOrThrowAsync(issueId);

        await _authGuard.AuthorizeProjectMember(issue);

        issue.Title = request.Title;
        issue.Description = request.Description;

        await _db.SaveChangesAsync();
        return issue.ToDto();
    }

    public async Task Delete(int projectId, int issueId)
    {
        await _db.Projects.ExistsOrThrowAsync(projectId);
        var issue = await _db.Issues.FindOrThrowAsync(issueId);

        await _authGuard.AuthorizeOwnerOrProjectLead(issue);

        await _attachmentsService.DeleteAll(issueId);
        _db.Issues.Remove(issue);

        await _db.SaveChangesAsync();

        _logger.LogInformation("User {userId} deleted issue {issueId}", _user.Id, issueId);
    }

    private IQueryable<int> GetVisibleProjectIds() =>
        _db.Projects
            .Where(p => p.Visibility == ProjectVisibility.Public || p.OwnerId == _user.Id
                || _db.ProjectMemberships.Any(m => m.ProjectId == p.Id && m.UserId == _user.Id))
            .Select(p => p.Id);

    private async Task<bool> IsVisibleToUser(Project project)
    {
        if (project.Visibility == ProjectVisibility.Public)
            return true;

        return project.OwnerId == _user.Id
            || await _db.ProjectMemberships.AnyAsync(m =>
                m.ProjectId == project.Id && m.UserId == _user.Id);
    }
}