namespace Sample.Components;

sealed class MotionPlaygroundPageState
{
    public int Recipe { get; set; }
}

sealed class MotionPlaygroundPage : Component<MotionPlaygroundPageState>
{
    static readonly RecipeSpec[] Recipes =
    [
        new("Fade in", box => box.Opacity = 0, m => m.FadeIn()),
        new("Fade out", box => box.Opacity = 1, m => m.FadeOut()),
        new("Slide in", box => { box.Opacity = 0; box.TranslationX = -40; }, m => m.FadeIn().SlideIn(SlideFrom.Left, 40)),
        new("Scale in", box => { box.Opacity = 0; box.ScaleX = 0.85; box.ScaleY = 0.85; }, m => m.FadeIn().ScaleIn()),
        new("Pulse", box => { box.ScaleX = 1; box.ScaleY = 1; }, m => m.Scale(1, 1.12)),
        new("Spin", box => box.Rotation = 0, m => m.Rotate(0, 180)),
        new("Bounce", box => { box.Opacity = 0; box.ScaleX = 0.8; box.ScaleY = 0.8; }, m => m
            .Opacity(k => k.At(0, 0).At(0.35, 1).At(1, 1))
            .Scale(k => k.At(0, 0.8).At(0.6, 1.08).At(1, 1))),
        new("Keyframe pulse", box => { box.ScaleX = 1; box.ScaleY = 1; }, m => m
            .Keyframes(
                (0.00, s => s.Scale(1)),
                (0.40, s => s.Scale(1.14)),
                (1.00, s => s.Scale(1)))),
    ];

    Microsoft.Maui.Controls.BoxView? _box;
    MotionPlayer? _player;

    public override VisualNode Render()
        => ContentPage(
            VStack(
                Label("Animate.Motion")
                    .FontSize(28)
                    .HCenter(),
                Label("Pick a recipe, then forward / reverse")
                    .FontSize(14)
                    .HCenter()
                    .TextColor(Colors.Gray),
                Picker()
                    .ItemsSource(Recipes.Select(r => r.Name).ToList())
                    .SelectedIndex(State.Recipe)
                    .OnSelectedIndexChanged(i => Select(i))
                    .HCenter(),
                BoxView(b => _box = b)
                    .HeightRequest(96)
                    .WidthRequest(96)
                    .CornerRadius(16)
                    .BackgroundColor(Colors.OrangeRed)
                    .HCenter()
                    .OnLoaded(() => Rebind(State.Recipe)),
                HStack(
                    Button("Forward", async () =>
                    {
                        if (_player is null) return;
                        await _player.ForwardAsync();
                    }),
                    Button("Pause", () => _player?.Pause()),
                    Button("Resume", () => _player?.Resume())
                )
                .Spacing(8)
                .HCenter(),
                HStack(
                    Button("Reverse", async () =>
                    {
                        if (_player is null) return;
                        await _player.ReverseAsync();
                    }),
                    Button("Reset", () =>
                    {
                        _player?.Reset();
                        ApplyRest(State.Recipe);
                    })
                )
                .Spacing(8)
                .HCenter()
            )
            .Spacing(16)
            .Padding(24)
            .VCenter()
        )
        .HideNavigationBar();

    void Select(int index)
    {
        var recipe = Math.Clamp(index, 0, Recipes.Length - 1);
        SetState(s => s.Recipe = recipe);
        Rebind(recipe);
    }

    void Rebind(int recipe)
    {
        _player?.Dispose();
        _player = null;
        if (_box is null)
            return;
        ApplyRest(recipe);
        _player = Animate.Motion.Bind(
            Recipes[recipe].Build(Motion.None).WithDuration(600).WithEasing(Easing.CubicOut),
            _box);
    }

    void ApplyRest(int recipe)
    {
        if (_box is null)
            return;
        _box.Opacity = 1;
        _box.TranslationX = 0;
        _box.TranslationY = 0;
        _box.ScaleX = 1;
        _box.ScaleY = 1;
        _box.Rotation = 0;
        Recipes[recipe].Rest(_box);
    }

    readonly record struct RecipeSpec(
        string Name,
        Action<Microsoft.Maui.Controls.BoxView> Rest,
        Func<Motion, Motion> Build);
}
