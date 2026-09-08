using Workbench.Common.Enums;
using Workbench.Common.Models;

namespace Workbench.Modules.Kanban.Models;

public class BoardColumn : IEntity<int>
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string? Description { get; set; }

    /// <summary>
    ///     The position of the column in the board. Lower numbers are further to the left.
    /// </summary>
    public required int Position { get; set; }

    /// <summary>
    ///     The maximum number of cards that can be in this column.
    /// </summary>
    public required int MaxCards { get; set; }

    /// <summary>
    ///     The color of the column, used for visual distinction in the UI.
    /// </summary>
    public required Color Color { get; set; }

    public required int BoardId { get; set; }
    public Board Board { get; set; } = null!;

    public ICollection<BoardCard> Cards { get; set; } = [];
}