using Workbench.Modules.Tags.Dtos;
using Workbench.Modules.Tags.Models;

namespace Workbench.Modules.Tags.Repositories;

public interface ITagsRepository
{
    Task<Tag?> FindAsync(int id);
    Task<Tag> GetByIdAsync(int id);
    Tag Add(Tag entity);
    Tag Update(Tag entity);
    void Remove(Tag entity);

    /// <summary>Returns all tags for the project with the given <paramref name="projectId" />.</summary>
    Task<List<TagDto>> GetAllByProjectIdAsync(int projectId);

    /// <summary>
    ///     Returns the tag whose name case-insensitively matches <paramref name="name" /> within the
    ///     project with the given <paramref name="projectId" />.
    /// </summary>
    Task<Tag?> FindByNameAsync(int projectId, string name);

    /// <summary>
    ///     Returns tags whose names are contained in <paramref name="names" /> within the project
    ///     with the given <paramref name="projectId" />.
    /// </summary>
    Task<List<Tag>> GetByNamesAsync(int projectId, IEnumerable<string> names);

    /// <summary>
    ///     Bulk-deletes the tag whose name case-insensitively matches <paramref name="name" /> within the
    ///     project with the given <paramref name="projectId" />.
    /// </summary>
    Task<int> DeleteByNameAsync(int projectId, string name);
}
