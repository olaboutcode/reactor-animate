namespace Reactor.Animate;

/// <summary>Where a <see cref="Motion.Stagger"/> delay is measured from.</summary>
public enum StaggerFrom
{
    /// <summary>Delay is <c>index * step</c>, in bind order.</summary>
    Start,

    /// <summary>
    /// Delay is the distance from the middle index. With a grid, it is the
    /// distance from the center of the grid.
    /// </summary>
    Center,

    /// <summary>Delay is <c>(count - 1 - index) * step</c>, in bind order.</summary>
    End,
}

/// <summary>
/// Delay between targets of one <see cref="Motion"/>. Applied only when more
/// than one view is bound to the root recipe.
/// </summary>
/// <param name="StepMilliseconds">Milliseconds between neighboring targets.</param>
/// <param name="From">Which end of the list or grid the first delay is measured from.</param>
/// <param name="Grid">
/// Column and row counts. Indexes walk the grid in row-major order.
/// <see cref="StaggerFrom.Center"/> uses cell distance; start and end stay in bind order.
/// </param>
public readonly record struct Stagger(
    uint StepMilliseconds,
    StaggerFrom From = StaggerFrom.Start,
    (int Columns, int Rows)? Grid = null);
