using Workbench.Common.Exceptions;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Issues.Dtos;
using Workbench.Modules.Issues.Repositories;
using Workbench.Modules.Milestones.Models;
using Workbench.Modules.Milestones.Repositories;

namespace Workbench.Modules.Milestones.Services.Implementations;

public class MilestoneIssuesService : IMilestoneIssuesService
{
    private readonly IAuthorizationGuard _authGuard;
    private readonly IIssuesRepository _issuesRepository;
    private readonly IMilestonesRepository _milestonesRepository;
    public MilestoneIssuesService(IMilestonesRepository milestonesRepository,
        IIssuesRepository issuesRepository,
        IAuthorizationGuard authGuard)
    {
        _milestonesRepository = milestonesRepository;
        _issuesRepository = issuesRepository;
        _authGuard = authGuard;
    }

    public async Task<List<IssueDto>> GetAllIssues(int projectId, int milestoneId)
    {
        var milestone = await _milestonesRepository.GetByIdAsync(milestoneId);
        ValidateProject(milestone, projectId);
        return await _milestonesRepository.GetAllIssuesAsync(milestoneId);
    }

    public async Task AddIssue(int projectId, int milestoneId, int issueId)
    {
        var milestone = await _milestonesRepository.FindForUpdateAsync(milestoneId)
                        ?? throw new NotFoundException($"Milestone with id {milestoneId} not found");
        ValidateProject(milestone, projectId);
        await _authGuard.AuthorizeProjectLead(milestone);

        var issue = await _issuesRepository.GetByIdAsync(issueId);
        if (issue.ProjectId != projectId)
            throw new BadRequestException("Issue does not belong to this project");

        if (milestone.MilestoneItems.Any(mi => mi.IssueId == issueId))
            throw new BadRequestException("Issue is already in this milestone");

        milestone.MilestoneItems.Add(new MilestoneItem
        {
            MilestoneId = milestoneId,
            IssueId = issueId
        });

        await _milestonesRepository.SaveChangesAsync();
    }

    public async Task RemoveIssue(int projectId, int milestoneId, int issueId)
    {
        var milestone = await _milestonesRepository.FindForUpdateAsync(milestoneId)
                        ?? throw new NotFoundException($"Milestone with id {milestoneId} not found");
        ValidateProject(milestone, projectId);
        await _authGuard.AuthorizeProjectLead(milestone);

        var item = milestone.MilestoneItems.FirstOrDefault(mi => mi.IssueId == issueId)
                   ?? throw new NotFoundException("Issue is not in this milestone");

        milestone.MilestoneItems.Remove(item);
        await _milestonesRepository.SaveChangesAsync();
    }

    private static void ValidateProject(Milestone milestone, int projectId)
    {
        if (milestone.ProjectId != projectId)
            throw new NotFoundException($"Milestone with id {milestone.Id} not found in project {projectId}");
    }
}
