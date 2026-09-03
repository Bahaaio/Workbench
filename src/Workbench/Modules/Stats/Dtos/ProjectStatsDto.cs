using Workbench.Modules.Issues.Enums;

namespace Workbench.Modules.Stats.Dtos;

public record ProjectStatsDto
{
    public required SummaryCardsDto Summary { get; init; }
    public required List<StatusBreakdownDto> IssueStatusBreakdown { get; init; }
    public required List<DailyCountDto> IssueCreationTrend { get; init; }
    public required List<MemberCountDto> IssuesPerMember { get; init; }
    public required List<TagCountDto> IssuesPerTag { get; init; }
    public required List<MilestoneProgressDto> MilestoneProgress { get; init; }
}

public record SummaryCardsDto
{
    public required int TotalIssues { get; init; }
    public required int OpenIssues { get; init; }
    public required int InProgressIssues { get; init; }
    public required int ClosedIssues { get; init; }
    public required int TotalMilestones { get; init; }
    public required int CompletedMilestones { get; init; }
    public required int OverdueMilestones { get; init; }
    public required int TotalMembers { get; init; }
}

public record StatusBreakdownDto
{
    public required Status Status { get; init; }
    public required int Count { get; init; }
}

public record DailyCountDto
{
    public required DateTime Date { get; init; }
    public required int Count { get; init; }
}

public record MemberCountDto
{
    public required string Username { get; init; }
    public required int Count { get; init; }
}

public record TagCountDto
{
    public required string Name { get; init; }
    public required string Color { get; init; }
    public required int Count { get; init; }
}

public record MilestoneProgressDto
{
    public required string Name { get; init; }
    public required int TotalItems { get; init; }
    public required int CompletedItems { get; init; }
    public required DateTime? DueDate { get; init; }
}
