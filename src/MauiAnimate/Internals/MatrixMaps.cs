using Reactor.Animate;

namespace Reactor.Animate.Internals;

/// <summary>
/// Converts a Flutter-style <see cref="Matrix4"/> into the row-vector layout
/// Core Animation and Windows composition use. Column vectors become a
/// transpose, then Z is flipped (<c>diag(1, 1, -1, 1)</c>) so
/// <c>SetEntry(3, 2, entry)</c> lands in <c>CATransform3D.m34</c> as <c>-entry</c>.
/// </summary>
internal static class MatrixMaps
{
    public static double RowMajor(Matrix4 matrix, int row, int column)
    {
        var value = matrix.Entry(column, row);
        if ((row == 2) ^ (column == 2))
            value = -value;
        return value;
    }

    /// <summary>
    /// Length of the perspective row. <see cref="Matrix4.RotateY"/> moves
    /// entry <c>(3, 2)</c> into another column, and the length stays the eye term.
    /// </summary>
    public static double PerspectiveStrength(Matrix4 matrix)
    {
        var x = matrix.Entry(3, 0);
        var y = matrix.Entry(3, 1);
        var z = matrix.Entry(3, 2);
        var length = Math.Sqrt((x * x) + (y * y) + (z * z));
        if (double.IsNaN(length) || double.IsInfinity(length))
            return 0;
        return length;
    }
}
