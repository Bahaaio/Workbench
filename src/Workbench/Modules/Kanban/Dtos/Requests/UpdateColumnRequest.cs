using System.ComponentModel.DataAnnotations;
using Workbench.Common.Enums;

namespace Workbench.Modules.Kanban.Dtos.Requests;

public record UpdateColumnRequest
{
    [Required] [MaxLength(100)] public required string Name { get; set; }
    [MaxLength(512)] public string? Description { get; set; }
    [Required] public required Color Color { get; set; }
    [Required] public required int MaxCards { get; set; }
}