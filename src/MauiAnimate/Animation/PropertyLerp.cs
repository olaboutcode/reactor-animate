using Microsoft.Maui.Controls.Shapes;

namespace Reactor.Animate.Animation;

internal static class PropertyLerp
{
    public static bool CanAnimate(BindableProperty property, object? first, object? last)
    {
        if (first is null || last is null)
            return false;

        var type = property.ReturnType;
        return type == typeof(double)
            || type == typeof(float)
            || type == typeof(Color)
            || type == typeof(Thickness)
            || type == typeof(CornerRadius)
            || type == typeof(Rect)
            || property == Border.StrokeShapeProperty;
    }

    public static void Push(VisualElement view, BindableProperty property)
        => view.Handler?.UpdateValue(property.PropertyName);

    public static CornerRadius RadiusOf(object value)
        => value switch
        {
            CornerRadius radius => radius,
            RoundRectangle round => round.CornerRadius,
            _ => default,
        };

    public static object? Lerp(object? from, object? to, double t)
        => Lerp(from, to, t, ColorSpace.Hsv);

    public static object? Lerp(object? from, object? to, double t, ColorSpace colorSpace)
    {
        if (from is double fd && to is double td)
            return fd + (td - fd) * t;

        if (from is float ff && to is float tf)
            return ff + (tf - ff) * t;

        if (from is Color fc && to is Color tc)
            return LerpColor(fc, tc, t, colorSpace);

        if (from is Thickness fth && to is Thickness tth)
            return new Thickness(
                fth.Left + (tth.Left - fth.Left) * t,
                fth.Top + (tth.Top - fth.Top) * t,
                fth.Right + (tth.Right - fth.Right) * t,
                fth.Bottom + (tth.Bottom - fth.Bottom) * t);

        if (from is Rect fr && to is Rect tr)
            return new Rect(
                fr.X + (tr.X - fr.X) * t,
                fr.Y + (tr.Y - fr.Y) * t,
                fr.Width + (tr.Width - fr.Width) * t,
                fr.Height + (tr.Height - fr.Height) * t);

        var fromRadius = from is Microsoft.Maui.CornerRadius or RoundRectangle ? RadiusOf(from) : (Microsoft.Maui.CornerRadius?)null;
        var toRadius = to is Microsoft.Maui.CornerRadius or RoundRectangle ? RadiusOf(to) : (Microsoft.Maui.CornerRadius?)null;
        if (fromRadius is { } a && toRadius is { } b)
            return new CornerRadius(
                a.TopLeft + (b.TopLeft - a.TopLeft) * t,
                a.TopRight + (b.TopRight - a.TopRight) * t,
                a.BottomLeft + (b.BottomLeft - a.BottomLeft) * t,
                a.BottomRight + (b.BottomRight - a.BottomRight) * t);

        return t < 1 ? from : to;
    }

    public static Color LerpRgb(Color from, Color to, double t)
        => Color.FromRgba(
            from.Red + (to.Red - from.Red) * t,
            from.Green + (to.Green - from.Green) * t,
            from.Blue + (to.Blue - from.Blue) * t,
            from.Alpha + (to.Alpha - from.Alpha) * t);

    public static Color LerpHsv(Color from, Color to, double t)
    {
        ToHsv(from, out var h0, out var s0, out var v0);
        ToHsv(to, out var h1, out var s1, out var v1);
        if (s0 < 1e-6)
            h0 = h1;
        if (s1 < 1e-6)
            h1 = h0;
        var dh = h1 - h0;
        if (dh > 0.5)
            dh -= 1;
        if (dh < -0.5)
            dh += 1;
        return FromHsv(
            h0 + dh * t,
            s0 + (s1 - s0) * t,
            v0 + (v1 - v0) * t,
            from.Alpha + (to.Alpha - from.Alpha) * t);
    }

    static Color LerpColor(Color from, Color to, double t, ColorSpace space)
        => space == ColorSpace.Rgb ? LerpRgb(from, to, t) : LerpHsv(from, to, t);

    static void ToHsv(Color color, out double h, out double s, out double v)
    {
        var r = color.Red;
        var g = color.Green;
        var b = color.Blue;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        v = max;
        var delta = max - min;
        s = max <= 0 ? 0 : delta / max;
        if (delta <= 1e-6)
        {
            h = 0;
            return;
        }

        if (max == r)
            h = (g - b) / delta + (g < b ? 6 : 0);
        else if (max == g)
            h = (b - r) / delta + 2;
        else
            h = (r - g) / delta + 4;
        h /= 6;
    }

    static Color FromHsv(double h, double s, double v, double a)
    {
        h = ((h % 1) + 1) % 1;
        s = Math.Clamp(s, 0, 1);
        v = Math.Clamp(v, 0, 1);
        a = Math.Clamp(a, 0, 1);
        var sector = h * 6;
        var i = (int)Math.Floor(sector);
        var f = sector - i;
        var p = v * (1 - s);
        var q = v * (1 - f * s);
        var t = v * (1 - (1 - f) * s);
        return (i % 6) switch
        {
            0 => Color.FromRgba(v, t, p, a),
            1 => Color.FromRgba(q, v, p, a),
            2 => Color.FromRgba(p, v, t, a),
            3 => Color.FromRgba(p, q, v, a),
            4 => Color.FromRgba(t, p, v, a),
            _ => Color.FromRgba(v, p, q, a),
        };
    }

    public static object ScaleLength(object value, double factor)
        => value switch
        {
            double d => d * factor,
            float f => (float)(f * factor),
            CornerRadius r => new CornerRadius(
                r.TopLeft * factor,
                r.TopRight * factor,
                r.BottomLeft * factor,
                r.BottomRight * factor),
            Thickness th => new Thickness(
                th.Left * factor,
                th.Top * factor,
                th.Right * factor,
                th.Bottom * factor),
            _ => value,
        };

    public static void Write(VisualElement view, BindableProperty property, object? value)
    {
        if (value is null)
            return;

        if (property == Border.StrokeShapeProperty)
        {
            view.SetValue(property, new RoundRectangle { CornerRadius = RadiusOf(value) });
            Push(view, property);
            return;
        }

        view.SetValue(property, value);
        if (NeedsHandlerPush(property))
            Push(view, property);
    }

    static bool NeedsHandlerPush(BindableProperty property)
        => property == Border.StrokeShapeProperty
            || property == BoxView.CornerRadiusProperty
            || property.PropertyName is "CornerRadius" or "StrokeShape";
}
