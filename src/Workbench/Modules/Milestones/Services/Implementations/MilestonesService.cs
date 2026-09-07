using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Milestones.Dtos;
using Workbench.Modules.Milestones.Dtos.Requests;
using Workbench.Modules.Milestones.Mappers;
using Workbench.Modules.Milestones.Models;
using Workbench.Modules.Projects.Models;

namespace Workbench.Modules.Milestones.Services.Implementations;

public class MilestonesService : IMilestonesService
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationGuard _authGuard;

    public MilestonesService(AppDbContext dbContext, IAuthorizationGuard authGuard)
    {
        _db = dbContext;
        _authGuard = authGuard;
    }

    public async Task<List<MilestoneDto>> GetAll(int projectId)
    {
        await _db.Projects.ExistsOrThrowAsync(projectId);
        return await _db.Milestones
            .Where(m => m.ProjectId == projectId)
            .Include(m => m.MilestoneItems)
                .ThenInclude(mi => mi.Issue)
            .Select(MilestoneMapper.ToDtoExpression)
            .ToListAsync();
    }

    public async Task<MilestoneDto> GetById(int projectId, int milestoneId)
    {
        var milestone = await _db.Milestones
            .Include(m => m.MilestoneItems)
                .ThenInclude(mi => mi.Issue)
            .SingleOrDefaultAsync(m => m.Id == milestoneId)
            ?? throw new NotFoundException($"Milestone with id {milestoneId} not found");

        ValidateProject(milestone, projectId);
        return milestone.ToDto();
    }

    public async Task<MilestoneDto> Create(int projectId, CreateMilestoneRequest request)
    {
        var project = await _db.Projects
            .Include(p => p.Owner)
            .SingleOrDefaultAsync(p => p.Id == projectId)
            ?? throw new NotFoundException($"Project with id {projectId} not found");

        await _authGuard.AuthorizeProjectLead(project);

        var milestone = new Milestone
        {
            ProjectId = projectId,
            Name = request.Name,
            Description = request.Description,
            DueDate = request.DueDate
        };

        _db.Milestones.Add(milestone);
        await _db.SaveChangesAsync();

        return milestone.ToDto();
    }

    public async Task<MilestoneDto> Update(int projectId, int milestoneId, UpdateMilestoneRequest request)
    {
        var milestone = await _db.Milestones
            .Include(m => m.MilestoneItems)
                .ThenInclude(mi => mi.Issue)
            .SingleOrDefaultAsync(m => m.Id == milestoneId)
            ?? throw new NotFoundException($"Milestone with id {milestoneId} not found");

        ValidateProject(milestone, projectId);
        await _authGuard.AuthorizeProjectLead(milestone);

        milestone.Name = request.Name;
        milestone.Description = request.Description;
        milestone.DueDate = request.DueDate;

        await _db.SaveChangesAsync();

        return milestone.ToDto();
    }

    public async Task Delete(int projectId, int milestoneId)
    {
        var milestone = await _db.Milestones
            .Include(m => m.MilestoneItems)
                .ThenInclude(mi => mi.Issue)
            .SingleOrDefaultAsync(m => m.Id == milestoneId)
            ?? throw new NotFoundException($"Milestone with id {milestoneId} not found");

        ValidateProject(milestone, projectId);
        await _authGuard.AuthorizeProjectLead(milestone);

        _db.Milestones.Remove(milestone);
        await _db.SaveChangesAsync();
    }

    private static void ValidateProject(Milestone milestone, int projectId)
    {
        if (milestone.ProjectId != projectId)
            throw new NotFoundException($"Milestone with id {milestone.Id} not found in project {projectId}");
    }
}
