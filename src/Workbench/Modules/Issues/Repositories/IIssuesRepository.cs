using Workbench.Modules.Issues.Dtos;
using Workbench.Modules.Issues.Dtos.Requests;
using Workbench.Modules.Issues.Models;

namespace Workbench.Modules.Issues.Repositories;

public interface IIssuesRepository
{
    Task<Issue?> FindAsync(int id);
    Task<Issue> GetByIdAsync(int id);
    Issue Add(Issue entity);
    Issue Update(Issue entity);
    void Remove(Issue entity);
    Task ExistsOrThrowAsync(int id);
    Task<List<IssueDto>> GetAllAsync(int projectId, IssueQuery query);

    /// <summary>
    ///     Returns an issue with all nav-props loaded for mutation (auth + update), or
    ///     <c>null</c>.
    /// </summary>
    Task<Issue?> FindForUpdateAsync(int id);

    /// <summary>Returns an issue with its <c>Tags</c> collection loaded, or <c>null</c>.</summary>
    Task<Issue?> FindWithTagsAsync(int id);

    /// <summary>
    ///     Returns all issues by <paramref name="authorId" /> matching <paramref name="query" />,
    ///     projected to DTOs.
    /// </summary>
    Task<List<IssueDto>> GetAllByAuthorAsync(int authorId, IssueQuery query);

    /// <summary>
    ///     Returns all issues assigned to <paramref name="userId" /> matching
    ///     <paramref name="query" />, projected to DTOs.
    /// </summary>
    Task<List<IssueDto>> GetAllAssignedToUserAsync(int userId, IssueQuery query);

    /// <summary>
    ///     Loads the <see cref="Issue.Author" /> navigation property for <paramref name="issue" />.
    ///     Used after insert to satisfy DTO mapping without a full re-fetch.
    /// </summary>
    Task LoadAuthorAsync(Issue issue);

    /// <summary>
    ///     Unassigns <paramref name="userId" /> from all non-closed issues in <paramref name="projectId" />.
    /// </summary>
    Task UnassignFromAllAsync(int projectId, int userId);
    Task SaveChangesAsync();
}
