namespace Sample.Components;

public class CustomButtonState
{
    public bool IsPressed { get; set; }
}

public partial class CustomButton : Component<CustomButtonState>
{
    [Prop]private string? _icon;
    [Prop]private double? _iconSize;
    [Prop]private double? _buttonSize;
    [Prop]private double? _cornerRadius;
    [Prop]private MauiReactor.Shadow? _buttonShadow;
    [Prop]private Func<Task>? _onTapped;
    [Prop]private Color _buttonColor = CustomColors.Gray600;

    public override VisualNode Render()
    {
        var container = Border(
            !(_icon is not null && _iconSize is not null)
            ? null
            : Image(_icon!)
            .HeightRequest(_iconSize!.Value)
            .WidthRequest(_iconSize!.Value)
            .Aspect(Aspect.AspectFit)
            .Center()
        )
        .Stroke(Colors.Transparent)
        .BackgroundColor(_buttonColor)
        .StrokeCornerRadius(_cornerRadius ?? Radius.Full)
        .HeightRequest(_buttonSize ?? TouchTarget.Comfortable)
        .WidthRequest(_buttonSize ?? TouchTarget.Comfortable)
        .OnPointerPressed((_, _) => SetState(state => state.IsPressed = true))
        .OnPointerReleased((_, _) => SetState(state => state.IsPressed = false))
        .Scale(State.IsPressed ? 0.98 : 1.0)
        .OnTapped(_onTapped);

        if (_buttonShadow is not null)
            container = container.Shadow(_buttonShadow);

        return container;
    }

    public static CustomButton Create(string icon) =>
        new() { _icon = icon };

    public static CustomButton ShareButton() =>
        NavButton(PhosphorIcons.ShareNetwork);

    public static CustomButton BackNavButton() =>
        NavButton(PhosphorIcons.ArrowLeft);

    public static CustomButton NavButton(string icon) =>
        Create(icon)
        .IconSize(IconSizing.Medium)
        .ButtonSize(TouchTarget.Min)
        .ButtonColor(Colors.WhiteSmoke)
        .CornerRadius(22)
        .ButtonShadow(new MauiReactor.Shadow()
            .Brush(Brush.Black)
            .Offset(0.3, 0.3)
            .Radius(4)
            .Opacity(0.28f)
        );
}
