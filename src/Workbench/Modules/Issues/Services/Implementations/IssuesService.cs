using Workbench.Modules.Attachments.Services;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Dtos;
using Workbench.Modules.Issues.Dtos.Requests;
using Workbench.Modules.Issues.Mappers;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Issues.Repositories;
using Workbench.Modules.Projects.Repositories;

namespace Workbench.Modules.Issues.Services.Implementations;

public class IssuesService : IIssuesService
{
    private readonly IAttachmentsService<Issue> _attachmentsService;
    private readonly IAuthorizationGuard _authGuard;
    private readonly IIssuesRepository _issuesRepository;
    private readonly ILogger<IssuesService> _logger;
    private readonly IProjectsRepository _projectsRepository;
    private readonly ICurrentUser _user;

    public IssuesService(IIssuesRepository issuesRepository,
        ICurrentUser user, IAuthorizationGuard authGuard, ILogger<IssuesService> logger,
        IAttachmentsService<Issue> attachmentsService, IProjectsRepository projectsRepository)
    {
        _issuesRepository = issuesRepository;
        _user = user;
        _authGuard = authGuard;
        _logger = logger;
        _attachmentsService = attachmentsService;
        _projectsRepository = projectsRepository;
    }

    public async Task<IssueDto> GetById(int projectId, int issueId)
    {
        await _projectsRepository.ExistsOrThrowAsync(projectId);
        return (await _issuesRepository.GetByIdAsync(issueId)).ToDto();
    }

    public async Task<List<IssueDto>> GetAll(int projectId, IssueQuery issueQuery)
    {
        await _projectsRepository.ExistsOrThrowAsync(projectId);
        return await _issuesRepository.GetAllAsync(projectId, issueQuery);
    }

    public Task<List<IssueDto>> GetCurrentUserIssues(IssueQuery issueQuery) =>
        _issuesRepository.GetAllByAuthorAsync(_user.Id, issueQuery);

    public async Task<IssueDto> Create(int projectId, CreateIssueRequest request)
    {
        var project = await _projectsRepository.GetByIdAsync(projectId);

        var issue = new Issue
        {
            Title = request.Title,
            Description = request.Description,
            AuthorId = _user.Id,
            ProjectId = projectId,
            Project = project
        };

        _issuesRepository.Add(issue);
        await _issuesRepository.SaveChangesAsync();

        _logger.LogInformation("User {userId} created issue {issueId}", _user.Id, issue.Id);

        await _issuesRepository.LoadAuthorAsync(issue);
        return issue.ToDto();
    }

    public async Task<IssueDto> Update(int projectId, int issueId, UpdateIssueRequest request)
    {
        await _projectsRepository.ExistsOrThrowAsync(projectId);
        var issue = await _issuesRepository.GetByIdAsync(issueId);

        await _authGuard.AuthorizeProjectMember(issue);

        issue.Title = request.Title;
        issue.Description = request.Description;

        await _issuesRepository.SaveChangesAsync();
        return issue.ToDto();
    }

    public async Task Delete(int projectId, int issueId)
    {
        await _projectsRepository.ExistsOrThrowAsync(projectId);
        var issue = await _issuesRepository.GetByIdAsync(issueId);

        await _authGuard.AuthorizeOwnerOrProjectLead(issue);

        await _attachmentsService.DeleteAll(issueId);
        _issuesRepository.Remove(issue);

        await _issuesRepository.SaveChangesAsync();

        _logger.LogInformation("User {userId} deleted issue {issueId}", _user.Id, issueId);
    }
}