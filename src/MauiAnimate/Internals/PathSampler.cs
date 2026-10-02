using Reactor.Animate;
using Microsoft.Maui.Controls.Shapes;

namespace Reactor.Animate.Internals;

// Arc-length table for a PathGeometry. Progress maps to distance along the path,
// not to the number of segments.
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
                    case ArcSegment arc:
                        AddArc(points, current, arc);
                        current = arc.Point;
                        break;
                }
            }
        }

        return points;
    }

    static void AddArc(List<Point> points, Point start, ArcSegment arc)
    {
        var end = arc.Point;
        var rx = Math.Abs(arc.Size.Width);
        var ry = Math.Abs(arc.Size.Height);
        if (rx < 1e-6 || ry < 1e-6 || Distance(start, end) < 1e-6)
        {
            points.Add(end);
            return;
        }

        var phi = arc.RotationAngle * Math.PI / 180;
        var cos = Math.Cos(phi);
        var sin = Math.Sin(phi);
        var dx = (start.X - end.X) / 2;
        var dy = (start.Y - end.Y) / 2;
        var x1p = cos * dx + sin * dy;
        var y1p = -sin * dx + cos * dy;

        var lambda = x1p * x1p / (rx * rx) + y1p * y1p / (ry * ry);
        if (lambda > 1)
        {
            var scale = Math.Sqrt(lambda);
            rx *= scale;
            ry *= scale;
        }

        var rx2 = rx * rx;
        var ry2 = ry * ry;
        var num = rx2 * ry2 - rx2 * y1p * y1p - ry2 * x1p * x1p;
        var den = rx2 * y1p * y1p + ry2 * x1p * x1p;
        var coeff = den <= 0 ? 0 : Math.Sqrt(Math.Max(0, num / den));
        var clockwise = arc.SweepDirection == SweepDirection.Clockwise;
        if (arc.IsLargeArc == clockwise)
            coeff = -coeff;

        var cxp = coeff * rx * y1p / ry;
        var cyp = coeff * -ry * x1p / rx;
        var cx = cos * cxp - sin * cyp + (start.X + end.X) / 2;
        var cy = sin * cxp + cos * cyp + (start.Y + end.Y) / 2;

        var theta1 = VectorAngle(1, 0, (x1p - cxp) / rx, (y1p - cyp) / ry);
        var dtheta = VectorAngle(
            (x1p - cxp) / rx, (y1p - cyp) / ry,
            (-x1p - cxp) / rx, (-y1p - cyp) / ry);

        if (!clockwise && dtheta > 0)
            dtheta -= 2 * Math.PI;
        if (clockwise && dtheta < 0)
            dtheta += 2 * Math.PI;

        var steps = Math.Max(8, (int)Math.Ceiling(Math.Abs(dtheta) / (Math.PI / 12)));
        for (var i = 1; i <= steps; i++)
        {
            var theta = theta1 + dtheta * (i / (double)steps);
            var x = rx * Math.Cos(theta);
            var y = ry * Math.Sin(theta);
            points.Add(new Point(
                cos * x - sin * y + cx,
                sin * x + cos * y + cy));
        }
    }

    static double VectorAngle(double ux, double uy, double vx, double vy)
    {
        var sign = ux * vy - uy * vx < 0 ? -1 : 1;
        var lu = Math.Sqrt(ux * ux + uy * uy);
        var lv = Math.Sqrt(vx * vx + vy * vy);
        if (lu < 1e-12 || lv < 1e-12)
            return 0;
        var c = Math.Clamp((ux * vx + uy * vy) / (lu * lv), -1, 1);
        return sign * Math.Acos(c);
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
