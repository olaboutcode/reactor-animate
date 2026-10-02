using Reactor.Animate;

namespace Reactor.Animate.Internals;

internal static class StaggerEval
{
    public static double DelayMs(int index, int count, Stagger stagger)
    {
        if (count <= 1 || stagger.StepMilliseconds == 0)
            return 0;
        return stagger.StepMilliseconds * IndexPrime(index, count, stagger);
    }

    public static double MaxDelayMs(int count, Stagger stagger)
    {
        if (count <= 1 || stagger.StepMilliseconds == 0)
            return 0;

        var max = 0d;
        for (var i = 0; i < count; i++)
            max = Math.Max(max, DelayMs(i, count, stagger));
        return max;
    }

    static double IndexPrime(int index, int count, Stagger stagger)
    {
        if (stagger.Grid is { } grid)
        {
            var columns = Math.Max(grid.Columns, 1);
            var rows = Math.Max(grid.Rows, 1);
            var row = index / columns;
            var column = index % columns;
            return stagger.From switch
            {
                StaggerFrom.End => Math.Max(count - 1 - index, 0),
                StaggerFrom.Center => Distance(column, row, (columns - 1) / 2.0, (rows - 1) / 2.0),
                _ => index,
            };
        }

        return stagger.From switch
        {
            StaggerFrom.End => Math.Max(count - 1 - index, 0),
            StaggerFrom.Center => Math.Abs(index - (count - 1) / 2.0),
            _ => index,
        };
    }

    static double Distance(double x, double y, double cx, double cy)
    {
        var dx = x - cx;
        var dy = y - cy;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
