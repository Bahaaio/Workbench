using Microsoft.EntityFrameworkCore;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Auth.Services;
using Workbench.Modules.Issues.Models;
using Workbench.Modules.Issues.Votes.Dtos;
using Workbench.Modules.Issues.Votes.Dtos.Requests;
using Workbench.Modules.Issues.Votes.Models;

namespace Workbench.Modules.Issues.Votes.Services.Implementations;

public class VotesService : IVotesService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public VotesService(AppDbContext dbContext, ICurrentUser user)
    {
        _db = dbContext;
        _user = user;
    }

    public async Task Vote(int issueId, VoteRequest request)
    {
        await _db.Issues.ExistsOrThrowAsync(issueId);
        var existingVote = await _db.Votes
            .FindAsync(issueId, _user.Id);

        if (existingVote is null)
            _db.Votes.Add(new Vote
            {
                Value = request.Vote,
                VoterId = _user.Id,
                IssueId = issueId
            });
        else
            existingVote.Value = request.Vote;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteUserVote(int issueId) =>
        await _db.Votes
            .Where(v => v.IssueId == issueId && v.VoterId == _user.Id)
            .ExecuteDeleteAsync();

    public async Task<VoteDto> GetUserVote(int issueId)
    {
        await _db.Issues.ExistsOrThrowAsync(issueId);

        var vote = await _db.Votes
            .FindAsync(issueId, _user.Id);
        return new VoteDto(vote?.Value);
    }
}
