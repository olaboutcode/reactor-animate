using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Maui.Controls.Shapes;

namespace Reactor.Animate.Page;

internal static class PropertyFlip
{
    static readonly ConcurrentDictionary<Type, BindableProperty[]> PropertiesByType = new();

    static readonly HashSet<BindableProperty> Excluded =
    [
        VisualElement.TranslationXProperty,
        VisualElement.TranslationYProperty,
        VisualElement.ScaleProperty,
        VisualElement.ScaleXProperty,
        VisualElement.ScaleYProperty,
        VisualElement.RotationProperty,
        VisualElement.RotationXProperty,
        VisualElement.RotationYProperty,
        VisualElement.AnchorXProperty,
        VisualElement.AnchorYProperty,
        VisualElement.WidthRequestProperty,
        VisualElement.HeightRequestProperty,
        VisualElement.MinimumWidthRequestProperty,
        VisualElement.MinimumHeightRequestProperty,
        VisualElement.MaximumWidthRequestProperty,
        VisualElement.MaximumHeightRequestProperty,
        VisualElement.IsVisibleProperty,
        VisualElement.InputTransparentProperty,
        VisualElement.OpacityProperty,
        VisualElement.ZIndexProperty,
        View.HorizontalOptionsProperty,
        View.VerticalOptionsProperty,
        View.MarginProperty,
    ];

    internal readonly record struct MorphStep(
        BindableProperty Property,
        object Look,
        object Rest,
        bool Length);

    public static void Morph(VisualElement flying, VisualElement invertAppearance, TweenBuilder tween, double scaleX)
        => Apply(flying, Plan(invertAppearance, flying), tween, scaleX);

    public static List<MorphStep> Plan(VisualElement invertAppearance, VisualElement rest)
    {
        var steps = new List<MorphStep>();
        foreach (var property in PropertiesFor(rest.GetType()))
        {
            if (!property.DeclaringType!.IsInstanceOfType(invertAppearance))
                continue;

            var look = Read(invertAppearance, property);
            var restValue = Read(rest, property);
            if (look is null || restValue is null || Equals(look, restValue))
                continue;
            if (!CanAnimate(property, look, restValue))
                continue;
            steps.Add(new MorphStep(property, look, restValue, IsLength(property, look)));
        }

        return steps;
    }

    public static void Apply(VisualElement flying, List<MorphStep> steps, TweenBuilder tween, double scaleX)
    {
        var factor = scaleX > 0 && scaleX < 1_000 ? 1 / scaleX : 1;
        foreach (var step in steps)
        {
            var invert = step.Length ? ScaleLength(step.Look, factor) : step.Look;
            Write(flying, step.Property, invert);
            tween.ToFlip(
                step.Property,
                step.Rest,
                invert,
                step.Look,
                step.Rest,
                step.Length ? scaleX : 1);
        }
    }

    static BindableProperty[] PropertiesFor(Type type)
        => PropertiesByType.GetOrAdd(type, static t =>
        {
            var list = new List<BindableProperty>();
            var seen = new HashSet<BindableProperty>();
            for (var current = t; current is not null && current != typeof(object); current = current.BaseType)
            {
                foreach (var field in current.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (field.FieldType != typeof(BindableProperty))
                        continue;

                    if (field.GetValue(null) is not BindableProperty property)
                        continue;

                    if (!seen.Add(property) || Excluded.Contains(property))
                        continue;

                    list.Add(property);
                }
            }

            return list.ToArray();
        });

    static bool CanAnimate(BindableProperty property, object? first, object? last)
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

    static bool IsLength(BindableProperty property, object value)
        => value is Microsoft.Maui.CornerRadius or Thickness
            || property == Label.FontSizeProperty
            || property == Border.StrokeThicknessProperty
            || property.PropertyName is "CornerRadius" or "FontSize" or "StrokeThickness" or "CharacterSpacing";

    static object? Read(VisualElement view, BindableProperty property)
    {
        if (property == Border.StrokeShapeProperty && view is Border { StrokeShape: RoundRectangle round })
            return round.CornerRadius;

        return view.GetValue(property);
    }

    static void Write(VisualElement view, BindableProperty property, object value)
    {
        if (property == Border.StrokeShapeProperty)
        {
            view.SetValue(property, new RoundRectangle { CornerRadius = RadiusOf(value) });
            Push(view, property);
            return;
        }

        view.SetValue(property, value);
        Push(view, property);
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
}
