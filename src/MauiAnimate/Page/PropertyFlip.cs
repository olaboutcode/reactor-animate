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

    public static void Morph(VisualElement flying, VisualElement invertAppearance, FlipTweenBuilder tween, double scaleX)
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
            if (!PropertyLerp.CanAnimate(property, look, restValue))
                continue;
            steps.Add(new MorphStep(property, look, restValue, IsLength(property, look)));
        }

        return steps;
    }

    public static void Apply(VisualElement flying, List<MorphStep> steps, FlipTweenBuilder tween, double scaleX)
    {
        var factor = scaleX > 0 && scaleX < 1_000 ? 1 / scaleX : 1;
        foreach (var step in steps)
        {
            var invert = step.Length ? PropertyLerp.ScaleLength(step.Look, factor) : step.Look;
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
            view.SetValue(property, new RoundRectangle { CornerRadius = PropertyLerp.RadiusOf(value) });
            PropertyLerp.Push(view, property);
            return;
        }

        view.SetValue(property, value);
        PropertyLerp.Push(view, property);
    }
}
