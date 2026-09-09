using System.Linq.Expressions;
using Workbench.Modules.Kanban.Dtos;
using Workbench.Modules.Kanban.Models;

namespace Workbench.Modules.Kanban.Mappers;

public static class ColumnMapper
{
    private static readonly Func<BoardColumn, ColumnDto> Compiled = ToDtoExpression.Compile();

    public static Expression<Func<BoardColumn, ColumnDto>> ToDtoExpression => c => new ColumnDto
    {
        Id = c.Id,
        Name = c.Name,
        Position = c.Position,
        Description = c.Description,
        MaxCards = c.MaxCards,
        Color = c.Color,
        Cards = c.Cards
            .AsQueryable()
            .OrderBy(bc => bc.Position)
            .Select(CardMapper.ToDtoExpression)
            .ToList()
    };

    public static ColumnDto ToDto(this BoardColumn column) => Compiled(column);
}