using Workbench.Modules.Stats.Dtos;

namespace Workbench.Modules.Stats.Repositories;

public interface IProjectStatsRepository
{
    Task<SummaryCardsDto> GetSummaryAsync(int projectId);
    Task<List<StatusBreakdownDto>> GetIssueStatusBreakdownAsync(int projectId);
    Task<List<DailyCountDto>> GetIssueCreationTrendAsync(int projectId, int days);
    Task<List<MemberCountDto>> GetIssuesPerMemberAsync(int projectId);
    Task<List<TagCountDto>> GetIssuesPerTagAsync(int projectId);
    Task<List<MilestoneProgressDto>> GetMilestoneProgressAsync(int projectId);
}
