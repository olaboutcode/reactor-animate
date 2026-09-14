using MauiReactor;
using MauiPage = Microsoft.Maui.Controls.Page;

namespace Reactor.Animate.Page;

internal static class Nav
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
            var destPage = navigation.NavigationStack[^1];
            var sourcePage = navigation.NavigationStack[^2];
            sourcePage.IsVisible = true;

            var flight = context.PopFlight();
            var prepared = flight is { } navFlight
                ? PrepareReturn(sourcePage, destPage, navFlight)
                : [];

            await FrameHold.CaptureAsync();
            try
            {
                await navigation.PopAsync(animated: false);

                IMotionClip? clip = null;
                if (flight is { } returning)
                {
                    clip = BuildReturnClip(sourcePage, returning, prepared);
                    if (clip is null)
                    {
                        await WaitForLayout(sourcePage);
                        clip = BuildReturnClip(sourcePage, returning, prepared);
                    }
                }

                await PlayHeld(clip);
            }
            finally
            {
                FrameHold.Release();
            }
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

            await FrameHold.CaptureAsync();
            var page = await push();
            try
            {
                await WaitForLayout(page);
                await WaitForHeroes(transition, snapshots);

                var clip = BuildClip(page, transition, snapshots);
                context.PushFlight(transition, snapshots);
                await PlayHeld(clip);
            }
            finally
            {
                FrameHold.Release();
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

        var heroes = new List<VisualElement>();
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
                transition,
                transition.ExtrasFor(tag),
                invertRotation: transition.ExtrasFor(tag).Rotation,
                morphFrom: snapshot.Source);
            if (flight is not null)
                clips.Add(flight);
            heroes.Add(hero);
        }

        var chrome = FadeChrome(page, heroes, transition);
        if (chrome is not null)
            clips.Add(chrome);

        return Combine(clips);
    }

    static async Task PlayHeld(IMotionClip? clip)
    {
        var playing = clip?.PlayAsync() ?? Task.CompletedTask;
        if (clip is not null && Application.Current?.Dispatcher is { } dispatcher)
            await dispatcher.DispatchAsync(static () => { });
        FrameHold.Release();
        await playing;
    }

    static List<ReturnPrep> PrepareReturn(MauiPage sourcePage, MauiPage destPage, NavFlight flight)
    {
        var context = HostContext.Current;
        var prepared = new List<ReturnPrep>();
        foreach (var snapshot in flight.Snapshots)
        {
            var sourceView = context.FindHeroOn(snapshot.Tag, sourcePage) ?? snapshot.Source;
            var destView = context.FindHeroOn(snapshot.Tag, destPage);
            if (sourceView is null || destView is null)
                continue;

            var destBounds = Geometry.GetWindowBounds(destView);
            if (destBounds.Width <= 0 || destBounds.Height <= 0)
                continue;

            prepared.Add(new ReturnPrep(
                snapshot.Tag,
                destBounds,
                PropertyFlip.Plan(destView, sourceView),
                flight.Transition.ExtrasFor(snapshot.Tag)));
        }

        return prepared;
    }

    static IMotionClip? BuildReturnClip(MauiPage sourcePage, NavFlight flight, List<ReturnPrep> prepared)
    {
        var clips = new List<IMotionClip>();
        var heroes = new List<VisualElement>();
        foreach (var prep in prepared)
        {
            var sourceView = HostContext.Current.FindHeroOn(prep.Tag, sourcePage);
            if (sourceView is null)
                continue;

            var clip = CreateFlipClip(
                sourceView,
                prep.DestBounds,
                sourcePage,
                flight.Transition,
                prep.Extras,
                invertRotation: 0,
                morph: prep.Morph);
            if (clip is not null)
                clips.Add(clip);
            heroes.Add(sourceView);
        }

        var chrome = FadeChrome(sourcePage, heroes, flight.Transition);
        if (chrome is not null)
            clips.Add(chrome);

        return Combine(clips);
    }

    static IMotionClip? FadeChrome(MauiPage page, List<VisualElement> heroes, Transition transition)
    {
        if (heroes.Count == 0)
            return null;

        var clips = new List<IMotionClip>();
        foreach (var view in ChromeViews(page, heroes))
        {
            view.Opacity = 0;
            view.Handler?.UpdateValue(nameof(VisualElement.Opacity));
            clips.Add(
                Motion.On(view)
                    .Owner(page)
                    .Duration(transition.Duration)
                    .Easing(Easing.CubicOut)
                    .Delay(0.7)
                    .To(VisualElement.OpacityProperty, 1d, 0d)
                    .Build());
        }

        return Combine(clips);
    }

    static IEnumerable<VisualElement> ChromeViews(Element root, IReadOnlyList<VisualElement> heroes)
    {
        if (root is VisualElement view && root is not MauiPage)
        {
            if (!KeepsHero(view, heroes))
            {
                yield return view;
                yield break;
            }
        }

        if (root is IVisualTreeElement tree)
        {
            foreach (var child in tree.GetVisualChildren())
            {
                if (child is not Element element)
                    continue;

                foreach (var chrome in ChromeViews(element, heroes))
                    yield return chrome;
            }
        }
    }

    static bool KeepsHero(VisualElement view, IReadOnlyList<VisualElement> heroes)
    {
        foreach (var hero in heroes)
        {
            if (ReferenceEquals(hero, view)
                || Geometry.IsUnder(hero, view)
                || Geometry.IsUnder(view, hero))
                return true;
        }

        return false;
    }

    readonly record struct ReturnPrep(
        string Tag,
        Rect DestBounds,
        List<PropertyFlip.MorphStep> Morph,
        MotionExtras Extras);

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
        Transition transition,
        MotionExtras extras,
        double invertRotation,
        VisualElement? morphFrom = null,
        List<PropertyFlip.MorphStep>? morph = null)
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
        var invertRotationValue = restRotation + invertRotation;

        flying.BatchBegin();
        flying.AnchorX = extras.AnchorX;
        flying.AnchorY = extras.AnchorY;
        flying.ScaleX = scaleX;
        flying.ScaleY = scaleY;
        flying.TranslationX = invertTranslationX;
        flying.TranslationY = invertTranslationY;
        if (invertRotation != 0)
            flying.Rotation = invertRotationValue;
        flying.BatchCommit();
        flying.Handler?.UpdateValue(nameof(VisualElement.TranslationX));
        flying.Handler?.UpdateValue(nameof(VisualElement.TranslationY));
        flying.Handler?.UpdateValue(nameof(VisualElement.ScaleX));
        flying.Handler?.UpdateValue(nameof(VisualElement.ScaleY));
        flying.Handler?.UpdateValue(nameof(VisualElement.AnchorX));
        flying.Handler?.UpdateValue(nameof(VisualElement.AnchorY));
        if (invertRotation != 0)
            flying.Handler?.UpdateValue(nameof(VisualElement.Rotation));

        var motion = Motion.On(flying)
            .Owner(owner)
            .Duration(transition.Duration)
            .Easing(transition.Easing)
            .To(VisualElement.ScaleXProperty, restScaleX, scaleX)
            .To(VisualElement.ScaleYProperty, restScaleY, scaleY)
            .To(VisualElement.TranslationXProperty, restTranslationX, invertTranslationX)
            .To(VisualElement.TranslationYProperty, restTranslationY, invertTranslationY);

        if (invertRotation != 0)
            motion.To(VisualElement.RotationProperty, restRotation, invertRotationValue);

        if (morph is not null)
            PropertyFlip.Apply(flying, morph, motion, scaleX);
        else if (morphFrom is not null)
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
