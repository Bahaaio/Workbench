using Workbench.Common.Exceptions;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Milestones.Dtos;
using Workbench.Modules.Milestones.Dtos.Requests;
using Workbench.Modules.Milestones.Mappers;
using Workbench.Modules.Milestones.Models;
using Workbench.Modules.Milestones.Repositories;
using Workbench.Modules.Projects.Repositories;

namespace Workbench.Modules.Milestones.Services.Implementations;

public class MilestonesService : IMilestonesService
{
    private readonly IAuthorizationGuard _authGuard;
    private readonly IMilestonesRepository _milestonesRepository;
    private readonly IProjectsRepository _projectsRepository;
    public MilestonesService(IMilestonesRepository milestonesRepository,
        IProjectsRepository projectsRepository,
        IAuthorizationGuard authGuard)
    {
        _milestonesRepository = milestonesRepository;
        _projectsRepository = projectsRepository;
        _authGuard = authGuard;
    }

    public async Task<List<MilestoneDto>> GetAll(int projectId)
    {
        await _projectsRepository.ExistsOrThrowAsync(projectId);
        return await _milestonesRepository.GetAllAsync(projectId);
    }

    public async Task<MilestoneDto> GetById(int projectId, int milestoneId)
    {
        var milestone = await _milestonesRepository.GetByIdAsync(milestoneId);
        ValidateProject(milestone, projectId);
        return milestone.ToDto();
    }

    public async Task<MilestoneDto> Create(int projectId, CreateMilestoneRequest request)
    {
        var project = await _projectsRepository.GetByIdAsync(projectId);
        await _authGuard.AuthorizeProjectLead(project);

        var milestone = new Milestone
        {
            ProjectId = projectId,
            Name = request.Name,
            Description = request.Description,
            DueDate = request.DueDate
        };

        _milestonesRepository.Add(milestone);
        await _milestonesRepository.SaveChangesAsync();

        return milestone.ToDto();
    }

    public async Task<MilestoneDto> Update(int projectId, int milestoneId, UpdateMilestoneRequest request)
    {
        var milestone = await _milestonesRepository.GetByIdAsync(milestoneId);
        ValidateProject(milestone, projectId);
        await _authGuard.AuthorizeProjectLead(milestone);

        milestone.Name = request.Name;
        milestone.Description = request.Description;
        milestone.DueDate = request.DueDate;

        await _milestonesRepository.SaveChangesAsync();

        return milestone.ToDto();
    }

    public async Task Delete(int projectId, int milestoneId)
    {
        var milestone = await _milestonesRepository.GetByIdAsync(milestoneId);
        ValidateProject(milestone, projectId);
        await _authGuard.AuthorizeProjectLead(milestone);

        _milestonesRepository.Remove(milestone);
        await _milestonesRepository.SaveChangesAsync();
    }

    private static void ValidateProject(Milestone milestone, int projectId)
    {
        if (milestone.ProjectId != projectId)
            throw new NotFoundException($"Milestone with id {milestone.Id} not found in project {projectId}");
    }
}
