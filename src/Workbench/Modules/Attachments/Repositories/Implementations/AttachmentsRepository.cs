using Workbench.Common.Exceptions;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Attachments.Models;
using Microsoft.EntityFrameworkCore;

namespace Workbench.Modules.Attachments.Repositories.Implementations;

/// <inheritdoc cref="IAttachmentsRepository{TAttachment}" />
public class AttachmentsRepository<TAttachment> : IAttachmentsRepository<TAttachment>
    where TAttachment : Attachment, IHasParent, new()
{
    private readonly AppDbContext _dbContext;
    private readonly DbSet<TAttachment> _dbSet;

    public AttachmentsRepository(AppDbContext context)
    {
        _dbContext = context;
        _dbSet = context.Set<TAttachment>();
    }

    public async Task<TAttachment?> FindAsync(Guid id) => await _dbSet.FindAsync(id);

    public Task<TAttachment> GetByIdAsync(Guid id) => _dbSet.FindOrThrowAsync(id);

    public TAttachment Add(TAttachment entity) => _dbSet.Add(entity).Entity;

    public void Remove(TAttachment entity) => _dbSet.Remove(entity);

    public Task<int> CountByParentIdAsync(int parentId) =>
        _dbSet.CountAsync(a => a.ParentId == parentId);

    public Task<List<string>> GetIdsByParentIdAsync(int parentId) =>
        _dbSet
            .Where(a => a.ParentId == parentId)
            .Select(a => a.Id.ToString())
            .ToListAsync();

    public async Task<int> GetParentIdByAttachmentAsync(Guid attachmentId) =>
        await _dbSet
            .Where(a => a.Id == attachmentId)
            .Select(a => (int?)a.ParentId)
            .SingleOrDefaultAsync()
        ?? throw new NotFoundException($"Attachment with id: {attachmentId} not found");

    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
