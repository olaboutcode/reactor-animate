namespace Sample.Components;

public sealed class AnimateButtonState { 
    public bool IsPressed { get; set; }
}

public partial class AnimateButton : Component<AnimateButtonState>
{
    [Prop]private string? _icon;
    [Prop]private string? _text;
    [Prop]private Func<Task>? _onClicked;
    [Prop]private Color _buttonColor = Colors.White;

    public override VisualNode Render()
    {
        // MAUI builds a shadow from opaque pixels. A transparent fill leaves only the icon and label.
        var fill = _buttonColor.Alpha > 0 ? _buttonColor : Colors.White;

        var container = Border(
            HStack(
                _icon is null
                ? null
                : Image(_icon)
                    .HeightRequest(IconSizing.Small)
                    .WidthRequest(IconSizing.Small)
                    .Aspect(Aspect.AspectFit)
                    .VCenter(),

                _text is null
                ? null
                : Label(_text)
                    .FontSize(FontSizing.Body)
                    .VCenter()
            )
            .Spacing(Spacing.Small)
            .Center()
        )
        .OnTapped(_onClicked)
        .Padding(Spacing.Small)
        .StrokeCornerRadius(_text is not null ? Radius.Small : Radius.Full)
        .HeightRequest(TouchTarget.Min)
        .OnPointerPressed((_, _) => SetState(state => state.IsPressed = true))
        .OnPointerReleased((_, _) => SetState(state => state.IsPressed = false))
        .Scale(State.IsPressed ? 0.98 : 1.0)
        .BackgroundColor(Color.FromArgb("#FAFAFA"))
        .StrokeThickness(0)
        .Shadow(
            new MauiReactor.Shadow()
                .Brush(Colors.Black)
                .Offset(0, 0)
                .Radius(3)
                .Opacity(0.3f)
        );

        if (_text is null)
            container.WidthRequest(TouchTarget.Min);

        return container;
    }
}
