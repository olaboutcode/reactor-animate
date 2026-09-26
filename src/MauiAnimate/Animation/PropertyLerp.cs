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
    {
        if (from is double fd && to is double td)
            return fd + (td - fd) * t;

        if (from is float ff && to is float tf)
            return ff + (tf - ff) * t;

        if (from is Color fc && to is Color tc)
            return Color.FromRgba(
                fc.Red + (tc.Red - fc.Red) * t,
                fc.Green + (tc.Green - fc.Green) * t,
                fc.Blue + (tc.Blue - fc.Blue) * t,
                fc.Alpha + (tc.Alpha - fc.Alpha) * t);

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
