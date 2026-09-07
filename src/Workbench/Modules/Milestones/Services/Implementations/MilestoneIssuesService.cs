using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Dtos;
using Workbench.Modules.Issues.Mappers;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Milestones.Models;

namespace Workbench.Modules.Milestones.Services.Implementations;

public class MilestoneIssuesService : IMilestoneIssuesService
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationGuard _authGuard;

    public MilestoneIssuesService(AppDbContext dbContext, IAuthorizationGuard authGuard)
    {
        _db = dbContext;
        _authGuard = authGuard;
    }

    public async Task<List<IssueDto>> GetAllIssues(int projectId, int milestoneId)
    {
        var milestone = await _db.Milestones
            .Include(m => m.MilestoneItems)
                .ThenInclude(mi => mi.Issue)
            .SingleOrDefaultAsync(m => m.Id == milestoneId)
            ?? throw new NotFoundException($"Milestone with id {milestoneId} not found");

        ValidateProject(milestone, projectId);

        return await _db.MilestoneItems
            .AsNoTracking()
            .Where(mi => mi.MilestoneId == milestoneId)
            .Select(mi => mi.Issue)
            .Select(IssueMapper.ToDtoExpression)
            .ToListAsync();
    }

    public async Task AddIssue(int projectId, int milestoneId, int issueId)
    {
        var milestone = await _db.Milestones
            .Include(m => m.MilestoneItems)
            .SingleOrDefaultAsync(m => m.Id == milestoneId)
            ?? throw new NotFoundException($"Milestone with id {milestoneId} not found");

        ValidateProject(milestone, projectId);
        await _authGuard.AuthorizeProjectLead(milestone);

        var issue = await _db.Issues.FindOrThrowAsync(issueId);
        if (issue.ProjectId != projectId)
            throw new BadRequestException("Issue does not belong to this project");

        if (milestone.MilestoneItems.Any(mi => mi.IssueId == issueId))
            throw new BadRequestException("Issue is already in this milestone");

        milestone.MilestoneItems.Add(new MilestoneItem
        {
            MilestoneId = milestoneId,
            IssueId = issueId
        });

        await _db.SaveChangesAsync();
    }

    public async Task RemoveIssue(int projectId, int milestoneId, int issueId)
    {
        var milestone = await _db.Milestones
            .Include(m => m.MilestoneItems)
            .SingleOrDefaultAsync(m => m.Id == milestoneId)
            ?? throw new NotFoundException($"Milestone with id {milestoneId} not found");

        ValidateProject(milestone, projectId);
        await _authGuard.AuthorizeProjectLead(milestone);

        var item = milestone.MilestoneItems.FirstOrDefault(mi => mi.IssueId == issueId)
                   ?? throw new NotFoundException("Issue is not in this milestone");

        milestone.MilestoneItems.Remove(item);
        await _db.SaveChangesAsync();
    }

    private static void ValidateProject(Milestone milestone, int projectId)
    {
        if (milestone.ProjectId != projectId)
            throw new NotFoundException($"Milestone with id {milestone.Id} not found in project {projectId}");
    }
}
