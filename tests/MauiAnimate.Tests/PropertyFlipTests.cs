using System.Reflection;
using Reactor.Animate.Internals;
using Xunit;

namespace MauiAnimate.Tests;

public sealed class PropertyFlipTests
{
    [Fact]
    public void Plan_skips_read_only_measured_size_when_the_ends_differ()
    {
        var look = new BoxView { Color = Colors.Red };
        var rest = new BoxView { Color = Colors.Navy };
        SetMeasured(look, "WidthPropertyKey", 88);
        SetMeasured(rest, "WidthPropertyKey", 240);
        SetMeasured(look, "HeightPropertyKey", 88);
        SetMeasured(rest, "HeightPropertyKey", 48);
        SetMeasured(look, "XPropertyKey", 12);
        SetMeasured(rest, "XPropertyKey", 40);
        SetMeasured(look, "YPropertyKey", 20);
        SetMeasured(rest, "YPropertyKey", 64);

        Assert.NotEqual(look.Width, rest.Width);
        Assert.NotEqual(look.Height, rest.Height);
        Assert.NotEqual(look.X, rest.X);
        Assert.NotEqual(look.Y, rest.Y);

        var steps = PropertyFlip.Plan(look, rest);

        Assert.Contains(steps, step => step.Property == BoxView.ColorProperty);
        Assert.DoesNotContain(steps, step => step.Property.IsReadOnly);
        Assert.DoesNotContain(steps, step => step.Property == VisualElement.WidthProperty);
        Assert.DoesNotContain(steps, step => step.Property == VisualElement.HeightProperty);
        Assert.DoesNotContain(steps, step => step.Property == VisualElement.XProperty);
        Assert.DoesNotContain(steps, step => step.Property == VisualElement.YProperty);
    }

    /// <summary>
    /// Layout is what normally writes these. The keys are private, so the test
    /// sets them the same way a measure pass does.
    /// </summary>
    static void SetMeasured(VisualElement view, string keyName, double value)
    {
        var key = typeof(VisualElement).GetField(keyName, BindingFlags.Static | BindingFlags.NonPublic)
            ?.GetValue(null) as BindablePropertyKey
            ?? throw new InvalidOperationException($"Missing {keyName}.");

        view.SetValue(key, value);
    }
}
