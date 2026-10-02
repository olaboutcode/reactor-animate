namespace Reactor.Animate;

/// <summary>
/// Column-major 4×4 matrix, matching Flutter <c>Matrix4</c>.
/// <see cref="Identity"/> is a new matrix on every read.
/// Each call post-multiplies, so the last call is applied to the point first.
/// <see cref="RotateX"/>, <see cref="RotateY"/>, and <see cref="RotateZ"/> take radians.
/// <see cref="Translate"/> is device-independent pixels.
/// <see cref="SetEntry"/> row 3, column 2 is the perspective term: the camera sits
/// <c>1/entry</c> device-independent pixels away. <c>0.002</c> is a mild depth.
/// <c>0</c> keeps both edges the same height.
/// </summary>
public sealed class Matrix4
{
    readonly double[] _v = new double[16];

    Matrix4()
    {
    }

    /// <summary>A new identity matrix.</summary>
    public static Matrix4 Identity
    {
        get
        {
            var matrix = new Matrix4();
            matrix._v[0] = 1;
            matrix._v[5] = 1;
            matrix._v[10] = 1;
            matrix._v[15] = 1;
            return matrix;
        }
    }

    /// <summary>Value at <paramref name="row"/> and <paramref name="column"/>, both 0–3.</summary>
    public double Entry(int row, int column)
    {
        if ((uint)row > 3 || (uint)column > 3)
            throw new ArgumentOutOfRangeException(nameof(row), "Row and column are 0–3.");
        return _v[(column * 4) + row];
    }

    /// <summary>
    /// Writes <paramref name="value"/> at <paramref name="row"/> and <paramref name="column"/>, both 0–3.
    /// <c>SetEntry(3, 2, 0.002)</c> is Flutter's perspective entry.
    /// </summary>
    public Matrix4 SetEntry(int row, int column, double value)
    {
        if ((uint)row > 3 || (uint)column > 3)
            throw new ArgumentOutOfRangeException(nameof(row), "Row and column are 0–3.");
        if (double.IsNaN(value) || double.IsInfinity(value))
            value = 0;
        _v[(column * 4) + row] = value;
        return this;
    }

    /// <summary>Post-multiplies a translation, in device-independent pixels.</summary>
    public Matrix4 Translate(double x, double y = 0, double z = 0)
    {
        var t1 = (_v[0] * x) + (_v[4] * y) + (_v[8] * z) + _v[12];
        var t2 = (_v[1] * x) + (_v[5] * y) + (_v[9] * z) + _v[13];
        var t3 = (_v[2] * x) + (_v[6] * y) + (_v[10] * z) + _v[14];
        var t4 = (_v[3] * x) + (_v[7] * y) + (_v[11] * z) + _v[15];
        _v[12] = t1;
        _v[13] = t2;
        _v[14] = t3;
        _v[15] = t4;
        return this;
    }

    /// <summary>Post-multiplies a rotation of <paramref name="radians"/> about X.</summary>
    public Matrix4 RotateX(double radians)
    {
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var t1 = (_v[4] * cos) + (_v[8] * sin);
        var t2 = (_v[5] * cos) + (_v[9] * sin);
        var t3 = (_v[6] * cos) + (_v[10] * sin);
        var t4 = (_v[7] * cos) + (_v[11] * sin);
        var t5 = (_v[4] * -sin) + (_v[8] * cos);
        var t6 = (_v[5] * -sin) + (_v[9] * cos);
        var t7 = (_v[6] * -sin) + (_v[10] * cos);
        var t8 = (_v[7] * -sin) + (_v[11] * cos);
        _v[4] = t1;
        _v[5] = t2;
        _v[6] = t3;
        _v[7] = t4;
        _v[8] = t5;
        _v[9] = t6;
        _v[10] = t7;
        _v[11] = t8;
        return this;
    }

    /// <summary>Post-multiplies a rotation of <paramref name="radians"/> about Y.</summary>
    public Matrix4 RotateY(double radians)
    {
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var t1 = (_v[0] * cos) + (_v[8] * -sin);
        var t2 = (_v[1] * cos) + (_v[9] * -sin);
        var t3 = (_v[2] * cos) + (_v[10] * -sin);
        var t4 = (_v[3] * cos) + (_v[11] * -sin);
        var t5 = (_v[0] * sin) + (_v[8] * cos);
        var t6 = (_v[1] * sin) + (_v[9] * cos);
        var t7 = (_v[2] * sin) + (_v[10] * cos);
        var t8 = (_v[3] * sin) + (_v[11] * cos);
        _v[0] = t1;
        _v[1] = t2;
        _v[2] = t3;
        _v[3] = t4;
        _v[8] = t5;
        _v[9] = t6;
        _v[10] = t7;
        _v[11] = t8;
        return this;
    }

    /// <summary>Post-multiplies a rotation of <paramref name="radians"/> about Z.</summary>
    public Matrix4 RotateZ(double radians)
    {
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var t1 = (_v[0] * cos) + (_v[4] * sin);
        var t2 = (_v[1] * cos) + (_v[5] * sin);
        var t3 = (_v[2] * cos) + (_v[6] * sin);
        var t4 = (_v[3] * cos) + (_v[7] * sin);
        var t5 = (_v[0] * -sin) + (_v[4] * cos);
        var t6 = (_v[1] * -sin) + (_v[5] * cos);
        var t7 = (_v[2] * -sin) + (_v[6] * cos);
        var t8 = (_v[3] * -sin) + (_v[7] * cos);
        _v[0] = t1;
        _v[1] = t2;
        _v[2] = t3;
        _v[3] = t4;
        _v[4] = t5;
        _v[5] = t6;
        _v[6] = t7;
        _v[7] = t8;
        return this;
    }

    /// <summary>Post-multiplies a uniform scale.</summary>
    public Matrix4 Scale(double factor)
        => Scale(factor, factor, factor);

    /// <summary>Post-multiplies a scale. Z defaults to 1.</summary>
    public Matrix4 Scale(double x, double y, double z = 1)
    {
        for (var row = 0; row < 4; row++)
        {
            _v[row] *= x;
            _v[4 + row] *= y;
            _v[8 + row] *= z;
        }

        return this;
    }
}
