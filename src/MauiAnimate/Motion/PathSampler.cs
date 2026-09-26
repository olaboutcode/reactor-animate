using Microsoft.Maui.Controls.Shapes;

namespace Reactor.Animate.Motion;

internal sealed class PathSampler
{
    readonly Point[] _points;
    readonly double[] _cum;
    readonly double _length;

    PathSampler(Point[] points, double[] cum, double length)
    {
        _points = points;
        _cum = cum;
        _length = length;
    }

    public static PathSampler? TryCreate(PathGeometry geometry)
    {
        var points = Flatten(geometry);
        if (points.Count < 2)
            return null;

        var cum = new double[points.Count];
        var length = 0d;
        for (var i = 1; i < points.Count; i++)
        {
            length += Distance(points[i - 1], points[i]);
            cum[i] = length;
        }

        if (length <= 0)
            return null;

        return new PathSampler([.. points], cum, length);
    }

    public Point PointAt(double t)
    {
        t = double.IsNaN(t) || double.IsInfinity(t) ? 0 : Math.Clamp(t, 0, 1);
        var d = t * _length;
        if (d <= 0)
            return _points[0];
        if (d >= _length)
            return _points[^1];

        var i = 1;
        while (i < _cum.Length && _cum[i] < d)
            i++;

        var prev = _cum[i - 1];
        var span = _cum[i] - prev;
        var s = span <= 0 ? 1 : (d - prev) / span;
        var a = _points[i - 1];
        var b = _points[i];
        return new Point(a.X + (b.X - a.X) * s, a.Y + (b.Y - a.Y) * s);
    }

    static List<Point> Flatten(PathGeometry geometry)
    {
        var points = new List<Point>();
        foreach (var figure in geometry.Figures)
        {
            var current = figure.StartPoint;
            points.Add(current);
            foreach (var segment in figure.Segments)
            {
                switch (segment)
                {
                    case LineSegment line:
                        current = line.Point;
                        points.Add(current);
                        break;
                    case PolyLineSegment poly:
                        foreach (var point in poly.Points)
                        {
                            current = point;
                            points.Add(current);
                        }
                        break;
                    case BezierSegment cubic:
                        AddCubic(points, current, cubic.Point1, cubic.Point2, cubic.Point3);
                        current = cubic.Point3;
                        break;
                    case PolyBezierSegment polyCubic:
                        for (var i = 0; i + 2 < polyCubic.Points.Count; i += 3)
                        {
                            AddCubic(points, current, polyCubic.Points[i], polyCubic.Points[i + 1], polyCubic.Points[i + 2]);
                            current = polyCubic.Points[i + 2];
                        }
                        break;
                    case QuadraticBezierSegment quad:
                        AddQuad(points, current, quad.Point1, quad.Point2);
                        current = quad.Point2;
                        break;
                    case PolyQuadraticBezierSegment polyQuad:
                        for (var i = 0; i + 1 < polyQuad.Points.Count; i += 2)
                        {
                            AddQuad(points, current, polyQuad.Points[i], polyQuad.Points[i + 1]);
                            current = polyQuad.Points[i + 1];
                        }
                        break;
                    case ArcSegment:
                        break;
                }
            }
        }

        return points;
    }

    static void AddCubic(List<Point> points, Point p0, Point p1, Point p2, Point p3)
    {
        for (var i = 1; i <= 16; i++)
            points.Add(Cubic(p0, p1, p2, p3, i / 16d));
    }

    static void AddQuad(List<Point> points, Point p0, Point p1, Point p2)
    {
        for (var i = 1; i <= 12; i++)
            points.Add(Quad(p0, p1, p2, i / 12d));
    }

    static Point Cubic(Point p0, Point p1, Point p2, Point p3, double t)
    {
        var u = 1 - t;
        var uu = u * u;
        var tt = t * t;
        return new Point(
            uu * u * p0.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + tt * t * p3.X,
            uu * u * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + tt * t * p3.Y);
    }

    static Point Quad(Point p0, Point p1, Point p2, double t)
    {
        var u = 1 - t;
        return new Point(
            u * u * p0.X + 2 * u * t * p1.X + t * t * p2.X,
            u * u * p0.Y + 2 * u * t * p1.Y + t * t * p2.Y);
    }

    static double Distance(Point a, Point b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
