using Workbench.Modules.Issues.Votes.Models;

namespace Workbench.Modules.Issues.Votes.Repositories;

/// <summary>
///     Bespoke repository for <see cref="Vote" />.
///     Vote uses a composite PK (IssueId + VoterId) and does not implement
///     <c>IEntity&lt;TKey&gt;</c>, so it cannot extend the generic <c>IRepository&lt;T, TKey&gt;</c>.
/// </summary>
public interface IVotesRepository
{
    /// <summary>Finds a vote by composite key, or <c>null</c>.</summary>
    Task<Vote?> FindAsync(int issueId, int userId);

    /// <summary>Stages a new vote for insert (does not save).</summary>
    void Add(Vote vote);

    /// <summary>Bulk-deletes the vote matching the composite key. No-op if not found.</summary>
    Task DeleteAsync(int issueId, int userId);

    /// <summary>Persists all pending changes to the database.</summary>
    Task SaveChangesAsync();
}
