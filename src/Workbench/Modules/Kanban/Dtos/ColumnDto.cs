using Workbench.Common.Enums;

namespace Workbench.Modules.Kanban.Dtos;

public record ColumnDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required int Position { get; init; }
    public required int MaxCards { get; init; }
    public required Color Color { get; init; }
    public required List<CardDto> Cards { get; init; }
}