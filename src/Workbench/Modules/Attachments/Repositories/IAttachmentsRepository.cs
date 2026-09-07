using Workbench.Modules.Attachments.Models;

namespace Workbench.Modules.Attachments.Repositories;

/// <summary>
///     Generic repository for typed attachment operations.
///     Handles all queries scoped to a specific <typeparamref name="TAttachment" /> (e.g.
///     IssueAttachment, CommentAttachment), plus base <see cref="Attachment" /> CRUD via the
///     shared TPH table.
/// </summary>
/// <typeparam name="TAttachment">The concrete attachment type.</typeparam>
public interface IAttachmentsRepository<TAttachment>
    where TAttachment : Attachment, new()
{
    Task<TAttachment?> FindAsync(Guid id);
    Task<TAttachment> GetByIdAsync(Guid id);
    TAttachment Add(TAttachment entity);
    void Remove(TAttachment entity);

    /// <summary>
    ///     Returns the number of attachments belonging to <paramref name="parentId" />
    /// </summary>
    Task<int> CountByParentIdAsync(int parentId);

    /// <summary>
    ///     Returns the Ids (string Guid) of all attachments belonging to <paramref name="parentId" />.
    /// </summary>
    Task<List<string>> GetIdsByParentIdAsync(int parentId);

    /// <summary>
    ///     Returns the <c>ParentId</c> of the attachment with <paramref name="attachmentId" />.
    /// </summary>
    Task<int> GetParentIdByAttachmentAsync(Guid attachmentId);
    Task SaveChangesAsync();
}
