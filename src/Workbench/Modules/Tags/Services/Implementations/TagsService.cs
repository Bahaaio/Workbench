using Microsoft.EntityFrameworkCore;
using Workbench.Common.Exceptions;
using Workbench.Common.Extensions;
using Workbench.Data;
using Workbench.Modules.Authorization.Extensions;
using Workbench.Modules.Authorization.Services;
using Workbench.Modules.Tags.Dtos;
using Workbench.Modules.Tags.Dtos.Requests;
using Workbench.Modules.Tags.Mappers;
using Workbench.Modules.Projects.Models;
using Workbench.Modules.Tags.Models;

namespace Workbench.Modules.Tags.Services.Implementations;

public class TagsService : ITagsService
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationGuard _authGuard;
    private readonly ILogger<TagsService> _logger;

    public TagsService(AppDbContext dbContext,
        ILogger<TagsService> logger,
        IAuthorizationGuard authGuard)
    {
        _db = dbContext;
        _logger = logger;
        _authGuard = authGuard;
    }

    public async Task<TagDto> Create(int projectId, CreateTagRequest request)
    {
        await AuthorizeProjectAccess(projectId);

        var existingTag = await _db.Tags
            .Where(t => t.ProjectId == projectId)
            .SingleOrDefaultAsync(t => EF.Functions.ILike(t.Name, request.Name));

        if (existingTag is not null)
            throw new ConflictException($"Tag with name {request.Name} already exists");

        var tag = new Tag
        {
            Name = request.Name.ToLower(),
            Description = request.Description,
            Color = request.Color,
            ProjectId = projectId
        };

        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created tag {tagName}", tag.Name);

        return tag.ToDto();
    }

    public Task<List<TagDto>> GetAll(int projectId) =>
        _db.Tags
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .Select(TagMapper.ToDtoExpression)
            .ToListAsync();

    public async Task<TagDto> Update(int projectId, string tagName, UpdateTagRequest request)
    {
        await AuthorizeProjectAccess(projectId);

        var tag = await _db.Tags
                    .Where(t => t.ProjectId == projectId)
                    .SingleOrDefaultAsync(t => EF.Functions.ILike(t.Name, tagName))
                  ?? throw new NotFoundException($"Tag with tagName {tagName} doesn't exist");

        tag.Description = request.Description;
        tag.Color = request.Color;

        await _db.SaveChangesAsync();
        return tag.ToDto();
    }

    public async Task Delete(int projectId, string tagName)
    {
        await AuthorizeProjectAccess(projectId);

        var deleted = await _db.Tags
            .Where(t => t.ProjectId == projectId)
            .Where(t => EF.Functions.ILike(t.Name, tagName))
            .ExecuteDeleteAsync();

        if (deleted > 0)
            _logger.LogInformation("Deleted tag {tagName}", tagName);
    }

    private async Task AuthorizeProjectAccess(int projectId)
    {
        var project = await _db.Projects
            .Include(p => p.Owner)
            .SingleOrDefaultAsync(p => p.Id == projectId)
            ?? throw new NotFoundException($"Project with id {projectId} not found");

        await _authGuard.AuthorizeOwnerOrProjectMember(project);
    }
}
