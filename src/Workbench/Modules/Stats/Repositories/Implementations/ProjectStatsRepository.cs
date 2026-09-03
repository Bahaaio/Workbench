using Microsoft.EntityFrameworkCore;
using Workbench.Data;
using Workbench.Modules.Issues.Enums;
using Workbench.Modules.Stats.Dtos;

namespace Workbench.Modules.Stats.Repositories.Implementations;

public class ProjectStatsRepository(AppDbContext db) : IProjectStatsRepository
{
    public async Task<SummaryCardsDto> GetSummaryAsync(int projectId)
    {
        var totalIssues = await db.Issues.CountAsync(i => i.ProjectId == projectId);
        var openIssues = await db.Issues.CountAsync(i => i.ProjectId == projectId && i.Status == Status.Open);
        var inProgressIssues = await db.Issues.CountAsync(i => i.ProjectId == projectId && i.Status == Status.InProgress);
        var closedIssues = await db.Issues.CountAsync(i => i.ProjectId == projectId && i.Status == Status.Closed);

        var milestones = await db.Milestones
            .Where(m => m.ProjectId == projectId)
            .Select(m => new
            {
                m.Id,
                m.DueDate,
                TotalItems = m.MilestoneItems.Count,
                CompletedItems = m.MilestoneItems.Count(mi => mi.Issue.Status == Status.Closed)
            })
            .ToListAsync();

        var totalMembers = await db.ProjectMemberships.CountAsync(m => m.ProjectId == projectId);

        return new SummaryCardsDto
        {
            TotalIssues = totalIssues,
            OpenIssues = openIssues,
            InProgressIssues = inProgressIssues,
            ClosedIssues = closedIssues,
            TotalMilestones = milestones.Count,
            CompletedMilestones = milestones.Count(m => m.TotalItems > 0 && m.CompletedItems == m.TotalItems),
            OverdueMilestones = milestones.Count(m =>
                m.DueDate.HasValue && m.DueDate.Value < DateTime.UtcNow &&
                m.TotalItems > 0 && m.CompletedItems < m.TotalItems),
            TotalMembers = totalMembers
        };
    }

    public async Task<List<StatusBreakdownDto>> GetIssueStatusBreakdownAsync(int projectId)
    {
        return await db.Issues
            .Where(i => i.ProjectId == projectId)
            .GroupBy(i => i.Status)
            .Select(g => new StatusBreakdownDto
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync();
    }

    public async Task<List<DailyCountDto>> GetIssueCreationTrendAsync(int projectId, int days)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);

        var dailyCounts = await db.Issues
            .Where(i => i.ProjectId == projectId && i.CreatedAt >= cutoff)
            .GroupBy(i => i.CreatedAt.Date)
            .Select(g => new DailyCountDto
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        return dailyCounts;
    }

    public async Task<List<MemberCountDto>> GetIssuesPerMemberAsync(int projectId)
    {
        return await db.Issues
            .Where(i => i.ProjectId == projectId && i.AssignedToId != null)
            .GroupBy(i => new { i.AssignedTo!.UserName })
            .Select(g => new MemberCountDto
            {
                Username = g.Key.UserName,
                Count = g.Count()
            })
            .OrderByDescending(m => m.Count)
            .ToListAsync();
    }

    public async Task<List<TagCountDto>> GetIssuesPerTagAsync(int projectId)
    {
        return await db.Tags
            .Where(t => t.ProjectId == projectId)
            .Select(t => new TagCountDto
            {
                Name = t.Name,
                Color = t.Color.ToString(),
                Count = t.Issues.Count(i => i.ProjectId == projectId)
            })
            .Where(t => t.Count > 0)
            .OrderByDescending(t => t.Count)
            .ToListAsync();
    }

    public async Task<List<MilestoneProgressDto>> GetMilestoneProgressAsync(int projectId)
    {
        return await db.Milestones
            .Where(m => m.ProjectId == projectId)
            .Select(m => new MilestoneProgressDto
            {
                Name = m.Name,
                TotalItems = m.MilestoneItems.Count,
                CompletedItems = m.MilestoneItems.Count(mi => mi.Issue.Status == Status.Closed),
                DueDate = m.DueDate
            })
            .OrderBy(m => m.DueDate)
            .ToListAsync();
    }
}
