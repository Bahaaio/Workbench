using Workbench.Modules.Projects.Repositories;
using Workbench.Modules.Stats.Dtos;
using Workbench.Modules.Stats.Repositories;

namespace Workbench.Modules.Stats.Services.Implementations;

public class ProjectStatsService(
    IProjectStatsRepository statsRepository,
    IProjectsRepository projectsRepository) : IProjectStatsService
{
    public async Task<ProjectStatsDto> GetProjectStatsAsync(int projectId)
    {
        await projectsRepository.ExistsOrThrowAsync(projectId);

        var summary = await statsRepository.GetSummaryAsync(projectId);
        var statusBreakdown = await statsRepository.GetIssueStatusBreakdownAsync(projectId);
        var creationTrend = await statsRepository.GetIssueCreationTrendAsync(projectId, 30);
        var perMember = await statsRepository.GetIssuesPerMemberAsync(projectId);
        var perTag = await statsRepository.GetIssuesPerTagAsync(projectId);
        var milestoneProgress = await statsRepository.GetMilestoneProgressAsync(projectId);

        return new ProjectStatsDto
        {
            Summary = summary,
            IssueStatusBreakdown = statusBreakdown,
            IssueCreationTrend = creationTrend,
            IssuesPerMember = perMember,
            IssuesPerTag = perTag,
            MilestoneProgress = milestoneProgress
        };
    }
}
