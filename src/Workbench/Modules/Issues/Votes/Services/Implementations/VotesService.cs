using Workbench.Modules.Auth.Services;
using Workbench.Modules.Issues.Repositories;
using Workbench.Modules.Issues.Votes.Dtos;
using Workbench.Modules.Issues.Votes.Dtos.Requests;
using Workbench.Modules.Issues.Votes.Models;
using Workbench.Modules.Issues.Votes.Repositories;

namespace Workbench.Modules.Issues.Votes.Services.Implementations;

public class VotesService : IVotesService
{
    private readonly IIssuesRepository _issuesRepository;
    private readonly ICurrentUser _user;
    private readonly IVotesRepository _votesRepository;

    public VotesService(IVotesRepository votesRepository, IIssuesRepository issuesRepository,
        ICurrentUser user)
    {
        _votesRepository = votesRepository;
        _issuesRepository = issuesRepository;
        _user = user;
    }

    public async Task Vote(int issueId, VoteRequest request)
    {
        await _issuesRepository.ExistsOrThrowAsync(issueId);
        var existingVote = await _votesRepository.FindAsync(issueId, _user.Id);

        if (existingVote is null)
            _votesRepository.Add(new Vote
            {
                Value = request.Vote,
                VoterId = _user.Id,
                IssueId = issueId
            });
        else
            existingVote.Value = request.Vote;

        await _votesRepository.SaveChangesAsync();
    }

    public Task DeleteUserVote(int issueId) =>
        _votesRepository.DeleteAsync(issueId, _user.Id);

    public async Task<VoteDto> GetUserVote(int issueId)
    {
        await _issuesRepository.ExistsOrThrowAsync(issueId);

        var vote = await _votesRepository.FindAsync(issueId, _user.Id);
        return new VoteDto(vote?.Value);
    }
}
