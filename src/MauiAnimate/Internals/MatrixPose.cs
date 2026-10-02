using Reactor.Animate;

namespace Reactor.Animate.Internals;

/// <summary>
/// Translation, scale, and Euler angles of a <see cref="Matrix4"/>, for platforms
/// that apply those as separate view properties. Angles are degrees.
/// A pure X, Y, or Z rotation comes back on that one axis.
/// </summary>
internal readonly record struct MatrixPose(
    double TranslationX,
    double TranslationY,
    double ScaleX,
    double ScaleY,
    double RotationX,
    double RotationY,
    double RotationZ,
    double Perspective)
{
    public static MatrixPose From(Matrix4 matrix)
    {
        var c0x = matrix.Entry(0, 0);
        var c0y = matrix.Entry(1, 0);
        var c0z = matrix.Entry(2, 0);
        var c1x = matrix.Entry(0, 1);
        var c1y = matrix.Entry(1, 1);
        var c1z = matrix.Entry(2, 1);
        var c2x = matrix.Entry(0, 2);
        var c2y = matrix.Entry(1, 2);
        var c2z = matrix.Entry(2, 2);

        var scaleX = Length(c0x, c0y, c0z);
        var scaleY = Length(c1x, c1y, c1z);
        var scaleZ = Length(c2x, c2y, c2z);
        if (scaleX > 1e-8)
        {
            c0x /= scaleX;
            c0y /= scaleX;
            c0z /= scaleX;
        }

        if (scaleY > 1e-8)
        {
            c1x /= scaleY;
            c1y /= scaleY;
            c1z /= scaleY;
        }

        if (scaleZ > 1e-8)
        {
            c2x /= scaleZ;
            c2y /= scaleZ;
            c2z /= scaleZ;
        }

        double rotationX;
        double rotationY;
        double rotationZ;
        if (Near(c0y, 0) && Near(c1x, 0) && Near(c1z, 0) && Near(c2y, 0))
        {
            rotationX = 0;
            rotationZ = 0;
            rotationY = Math.Atan2(c2x, c0x);
        }
        else if (Near(c0y, 0) && Near(c0z, 0) && Near(c1x, 0) && Near(c2x, 0))
        {
            rotationY = 0;
            rotationZ = 0;
            rotationX = Math.Atan2(c1z, c2z);
        }
        else if (Near(c0z, 0) && Near(c1z, 0) && Near(c2x, 0) && Near(c2y, 0))
        {
            rotationX = 0;
            rotationY = 0;
            rotationZ = Math.Atan2(c0y, c0x);
        }
        else
        {
            var sy = Math.Sqrt((c0x * c0x) + (c0y * c0y));
            if (sy > 1e-6)
            {
                rotationX = Math.Atan2(c1z, c2z);
                rotationY = Math.Atan2(-c0z, sy);
                rotationZ = Math.Atan2(c0y, c0x);
            }
            else
            {
                rotationX = Math.Atan2(-c2y, c1y);
                rotationY = Math.Atan2(-c0z, sy);
                rotationZ = 0;
            }
        }

        var w = matrix.Entry(3, 3);
        if (Math.Abs(w) < 1e-8)
            w = 1;

        return new MatrixPose(
            matrix.Entry(0, 3) / w,
            matrix.Entry(1, 3) / w,
            scaleX,
            scaleY,
            rotationX * (180.0 / Math.PI),
            rotationY * (180.0 / Math.PI),
            rotationZ * (180.0 / Math.PI),
            MatrixMaps.PerspectiveStrength(matrix));
    }

    static bool Near(double value, double target)
        => Math.Abs(value - target) < 1e-6;

    static double Length(double x, double y, double z)
    {
        var length = Math.Sqrt((x * x) + (y * y) + (z * z));
        return double.IsNaN(length) || double.IsInfinity(length) ? 0 : length;
    }
}
