using Workbench.Common.Exceptions;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Projects.Repositories;
using Workbench.Modules.Tags.Dtos;
using Workbench.Modules.Tags.Dtos.Requests;
using Workbench.Modules.Tags.Mappers;
using Workbench.Modules.Tags.Models;
using Workbench.Modules.Tags.Repositories;

namespace Workbench.Modules.Tags.Services.Implementations;

public class TagsService : ITagsService
{
    private readonly IAuthorizationGuard _authGuard;
    private readonly ILogger<TagsService> _logger;
    private readonly IProjectsRepository _projectsRepository;
    private readonly ITagsRepository _tagsRepository;
    public TagsService(ITagsRepository tagsRepository,
        ILogger<TagsService> logger, IProjectsRepository projectsRepository,
        IAuthorizationGuard authGuard)
    {
        _tagsRepository = tagsRepository;
        _logger = logger;
        _projectsRepository = projectsRepository;
        _authGuard = authGuard;
    }

    public async Task<TagDto> Create(int projectId, CreateTagRequest request)
    {
        await AuthorizeProjectAccess(projectId);

        var existingTag = await _tagsRepository.FindByNameAsync(projectId, request.Name);
        if (existingTag is not null)
            throw new ConflictException($"Tag with name {request.Name} already exists");

        var tag = new Tag
        {
            Name = request.Name.ToLower(),
            Description = request.Description,
            Color = request.Color,
            ProjectId = projectId
        };

        _tagsRepository.Add(tag);
        await _tagsRepository.SaveChangesAsync();

        _logger.LogInformation("Created tag {tagName}", tag.Name);

        return tag.ToDto();
    }

    public Task<List<TagDto>> GetAll(int projectId) =>
        _tagsRepository.GetAllByProjectIdAsync(projectId);

    public async Task<TagDto> Update(int projectId, string tagName, UpdateTagRequest request)
    {
        await AuthorizeProjectAccess(projectId);

        var tag = await _tagsRepository.FindByNameAsync(projectId, tagName)
                  ?? throw new NotFoundException($"Tag with tagName {tagName} doesn't exist");

        tag.Description = request.Description;
        tag.Color = request.Color;

        await _tagsRepository.SaveChangesAsync();
        return tag.ToDto();
    }

    public async Task Delete(int projectId, string tagName)
    {
        await AuthorizeProjectAccess(projectId);

        var deleted = await _tagsRepository.DeleteByNameAsync(projectId, tagName);

        if (deleted > 0)
            _logger.LogInformation("Deleted tag {tagName}", tagName);
    }

    /// <summary>
    ///     Authorizes the current user to access the project with the given <paramref name="projectId" />.
    /// </summary>
    private async Task AuthorizeProjectAccess(int projectId)
    {
        var project = await _projectsRepository.GetByIdAsync(projectId);
        await _authGuard.AuthorizeOwnerOrProjectMember(project);
    }
}