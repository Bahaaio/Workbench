using System.ComponentModel.DataAnnotations;
using Workbench.Modules.Projects.Enums;

namespace Workbench.Modules.Projects.Dtos.Requests;

public record CreateProjectRequest
{
    [Required] [MaxLength(100)] public required string Name { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    public ProjectVisibility Visibility { get; set; }
}