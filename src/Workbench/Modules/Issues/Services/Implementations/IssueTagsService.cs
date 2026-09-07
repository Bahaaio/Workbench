using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Data;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Tags.Models;

namespace Workbench.Modules.Issues.Services.Implementations;

public class IssueTagsService : IIssueTagsService
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationGuard _authGuard;

    public IssueTagsService(AppDbContext dbContext,
        IAuthorizationGuard authGuard)
    {
        _db = dbContext;
        _authGuard = authGuard;
    }

    public async Task<List<string>> UpdateTags(int issueId, List<string> tags)
    {
        var issue = await _db.Issues
                    .Include(i => i.Tags)
                    .Where(i => i.Id == issueId)
                    .SingleOrDefaultAsync()
                    ?? throw new NotFoundException($"Issue with id {issueId} not found");
        await _authGuard.AuthorizeProjectMember(issue);

        var lowerTags = tags.Select(n => n.ToLower()).ToList();
        var tagEntities = await _db.Tags
            .Where(t => t.ProjectId == issue.ProjectId)
            .Where(t => lowerTags.Contains(t.Name))
            .ToListAsync();

        var missing = lowerTags.Except(tagEntities.Select(t => t.Name)).ToList();
        if (missing.Count != 0)
            throw new NotFoundException($"Tags {string.Join(", ", missing)} not found");

        issue.Tags = tagEntities;
        await _db.SaveChangesAsync();

        return issue.Tags.Select(t => t.Name).ToList();
    }
}