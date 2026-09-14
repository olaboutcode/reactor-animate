using MauiReactor;
using MauiPage = Microsoft.Maui.Controls.Page;

namespace Reactor.Animate;

static class Nav
{
    public static Task<MauiPage> PushAsync<TPage>(Transition? transition = null)
        where TPage : Component, new()
    {
        var navigation = HostContext.Current.RequireNavigation();
        return PushCore(
            navigation,
            transition ?? Transition.None,
            async () => await navigation.PushAsync<TPage>(animated: false)
                ?? throw new InvalidOperationException("Navigation.PushAsync returned no page."));
    }

    public static Task<MauiPage> PushAsync<TPage, TProps>(
        Transition transition,
        Action<TProps> props)
        where TPage : Component, new()
        where TProps : class, new()
    {
        var navigation = HostContext.Current.RequireNavigation();
        return PushCore(
            navigation,
            transition,
            async () => await navigation.PushAsync<TPage, TProps>(animated: false, props)
                ?? throw new InvalidOperationException("Navigation.PushAsync returned no page."));
    }

    public static async Task PopAsync()
    {
        var navigation = HostContext.Current.RequireNavigation();
        var context = HostContext.Current;
        if (context.IsBusy)
            return;

        if (navigation.NavigationStack.Count <= 1)
            return;

        context.IsBusy = true;
        try
        {
            var popped = 0;
            void PopNow()
            {
                if (Interlocked.Exchange(ref popped, 1) != 0)
                    return;
                _ = navigation.PopAsync(animated: false);
            }

            var clip = context.PopClip();
            if (clip is not null)
            {
                clip.NotifyWhenSettled(PopNow);
                await clip.ReverseAsync();
            }

            if (Interlocked.Exchange(ref popped, 1) == 0)
                await navigation.PopAsync(animated: false);
        }
        finally
        {
            context.IsBusy = false;
        }
    }

    static async Task<MauiPage> PushCore(
        INavigation? navigation,
        Transition transition,
        Func<Task<MauiPage>> push)
    {
        ArgumentNullException.ThrowIfNull(navigation);

        var context = HostContext.Current;
        if (context.IsBusy)
            return navigation.NavigationStack[^1];

        context.IsBusy = true;
        try
        {
            var snapshots = transition.Tags
                .Select(context.Snapshot)
                .OfType<HeroSnapshot>()
                .ToArray();

            var page = await push();
            page.Opacity = 0;
            try
            {
                await WaitForLayout(page);
                await WaitForHeroes(transition, snapshots);

                var clip = BuildClip(page, transition, snapshots);
                if (clip is not null)
                    context.PushClip(clip);

                page.Opacity = 1;

                if (clip is not null)
                    await clip.PlayAsync();
            }
            finally
            {
                page.Opacity = 1;
            }

            return page;
        }
        finally
        {
            context.IsBusy = false;
        }
    }

    static IMotionClip? BuildClip(MauiPage page, Transition transition, HeroSnapshot[] snapshots)
    {
        var clips = new List<IMotionClip>();
        var snapshotByTag = snapshots
            .GroupBy(s => s.Tag, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.Ordinal);

        foreach (var tag in transition.Tags)
        {
            if (!snapshotByTag.TryGetValue(tag, out var snapshot))
                continue;

            var hero = HostContext.Current.FindHero(tag, snapshot.Source);
            if (hero is null)
                continue;

            var flight = CreateFlipClip(
                hero,
                snapshot.WindowBounds,
                page,
                snapshot.Source,
                transition,
                transition.ExtrasFor(tag));
            if (flight is not null)
                clips.Add(flight);
        }

        return Combine(clips);
    }

    static IMotionClip? Combine(List<IMotionClip> clips)
        => clips.Count switch
        {
            0 => null,
            1 => clips[0],
            _ => Motion.Parallel([.. clips]),
        };

    static IMotionClip? CreateFlipClip(
        VisualElement flying,
        Rect lookLike,
        MauiPage owner,
        VisualElement morphFrom,
        Transition transition,
        MotionExtras extras)
    {
        var rest = Geometry.GetWindowBounds(flying);
        if (rest.Width <= 0 || rest.Height <= 0 || lookLike.Width <= 0 || lookLike.Height <= 0)
            return null;

        var scaleX = lookLike.Width / rest.Width;
        var scaleY = lookLike.Height / rest.Height;
        var restTranslationX = flying.TranslationX;
        var restTranslationY = flying.TranslationY;
        var restScaleX = flying.ScaleX;
        var restScaleY = flying.ScaleY;
        var restRotation = flying.Rotation;
        var invertTranslationX = lookLike.X - rest.X
            - extras.AnchorX * rest.Width * (1 - scaleX);
        var invertTranslationY = lookLike.Y - rest.Y
            - extras.AnchorY * rest.Height * (1 - scaleY);
        var invertRotation = restRotation + extras.Rotation;

        flying.BatchBegin();
        flying.AnchorX = extras.AnchorX;
        flying.AnchorY = extras.AnchorY;
        flying.ScaleX = scaleX;
        flying.ScaleY = scaleY;
        flying.TranslationX = invertTranslationX;
        flying.TranslationY = invertTranslationY;
        if (extras.Rotation != 0)
            flying.Rotation = invertRotation;
        flying.BatchCommit();

        var motion = Motion.On(flying)
            .Owner(owner)
            .Duration(transition.Duration)
            .Easing(transition.Easing)
            .To(VisualElement.ScaleXProperty, restScaleX, scaleX)
            .To(VisualElement.ScaleYProperty, restScaleY, scaleY)
            .To(VisualElement.TranslationXProperty, restTranslationX, invertTranslationX)
            .To(VisualElement.TranslationYProperty, restTranslationY, invertTranslationY);

        if (extras.Rotation != 0)
            motion.To(VisualElement.RotationProperty, restRotation, invertRotation);

        PropertyFlip.Morph(flying, morphFrom, motion, scaleX);

        return motion.Build();
    }

    static async Task WaitForLayout(MauiPage page)
    {
        if (page.Width <= 0 || page.Height <= 0)
        {
            var tcs = new TaskCompletionSource();
            void OnSize(object? sender, EventArgs e)
            {
                if (page.Width > 0 && page.Height > 0)
                    tcs.TrySetResult();
            }

            page.SizeChanged += OnSize;
            try
            {
                await Task.WhenAny(tcs.Task, Task.Delay(500));
            }
            finally
            {
                page.SizeChanged -= OnSize;
            }
        }

        if (page.Dispatcher is { } dispatcher)
            await dispatcher.DispatchAsync(static () => { });
    }

    static async Task WaitForHeroes(Transition transition, HeroSnapshot[] snapshots)
    {
        if (transition.Tags.Count == 0)
            return;

        var deadline = Environment.TickCount64 + 500;
        while (Environment.TickCount64 < deadline)
        {
            var ready = true;
            foreach (var tag in transition.Tags)
            {
                VisualElement? source = null;
                foreach (var snapshot in snapshots)
                {
                    if (snapshot.Tag == tag)
                        source = snapshot.Source;
                }

                if (HostContext.Current.FindHero(tag, source) is null)
                {
                    ready = false;
                    break;
                }
            }

            if (ready)
                return;

            await Task.Delay(16);
        }
    }
}
