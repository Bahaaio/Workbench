using Workbench.Modules.Comments.Dtos;
using Workbench.Modules.Comments.Models;

namespace Workbench.Modules.Comments.Repositories;

public interface ICommentsRepository
{
    Task<Comment> GetByIdAsync(int id);
    Comment Add(Comment entity);
    void Remove(Comment entity);
    Task<List<CommentDto>> GetAllByIssueIdAsync(int issueId);
}
