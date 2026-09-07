using Workbench.Modules.Issues.Dtos;
using Workbench.Modules.Milestones.Dtos;
using Workbench.Modules.Milestones.Models;

namespace Workbench.Modules.Milestones.Repositories;

public interface IMilestonesRepository
{
    Task<Milestone?> FindAsync(int id);
    Task<Milestone> GetByIdAsync(int id);
    Milestone Add(Milestone entity);
    Milestone Update(Milestone entity);
    void Remove(Milestone entity);
    Task<List<MilestoneDto>> GetAllAsync(int projectId);

    /// <summary>
    ///     Returns a milestone with <c>MilestoneItems</c> loaded for mutation, or <c>null</c>.
    /// </summary>
    Task<Milestone?> FindForUpdateAsync(int milestoneId);

    /// <summary>
    ///     Returns a milestone with <c>MilestoneItems</c> and their <c>Issue</c> loaded, or <c>null</c>.
    /// </summary>
    Task<Milestone?> FindWithItemsAsync(int milestoneId);

    /// <summary>Returns all issues in a milestone, projected to DTOs.</summary>
    Task<List<IssueDto>> GetAllIssuesAsync(int milestoneId);
    Task SaveChangesAsync();
}
