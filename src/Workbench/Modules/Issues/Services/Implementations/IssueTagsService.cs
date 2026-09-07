using Workbench.Common.Exceptions;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Repositories;
using Workbench.Modules.Tags.Repositories;

namespace Workbench.Modules.Issues.Services.Implementations;

public class IssueTagsService : IIssueTagsService
{
    private readonly IAuthorizationGuard _authGuard;
    private readonly IIssuesRepository _issuesRepository;
    private readonly ITagsRepository _tagsRepository;
    public IssueTagsService(IIssuesRepository issuesRepository, ITagsRepository tagsRepository,
        IAuthorizationGuard authGuard)
    {
        _issuesRepository = issuesRepository;
        _tagsRepository = tagsRepository;
        _authGuard = authGuard;
    }

    public async Task<List<string>> UpdateTags(int issueId, List<string> tags)
    {
        var issue = await _issuesRepository.FindWithTagsAsync(issueId)
                    ?? throw new NotFoundException($"Issue with id {issueId} not found");
        await _authGuard.AuthorizeProjectMember(issue);

        var lowerTags = tags.Select(n => n.ToLower()).ToList();
        var tagEntities = await _tagsRepository.GetByNamesAsync(issue.ProjectId, lowerTags);

        var missing = lowerTags.Except(tagEntities.Select(t => t.Name)).ToList();
        if (missing.Count != 0)
            throw new NotFoundException($"Tags {string.Join(", ", missing)} not found");

        issue.Tags = tagEntities;
        await _issuesRepository.SaveChangesAsync();

        return issue.Tags.Select(t => t.Name).ToList();
    }
}