using MauiReactor;
using MauiPage = Microsoft.Maui.Controls.Page;

namespace Reactor.Animate;

static class Nav
{
    public static Task<MauiPage> PushAsync<TPage>(
        INavigation? navigation,
        Transition? transition = null)
        where TPage : Component, new()
        => PushCore(
            navigation,
            transition ?? Transition.None,
            async () => await navigation.PushAsync<TPage>(animated: false)
                ?? throw new InvalidOperationException("Navigation.PushAsync returned no page."));

    public static Task<MauiPage> PushAsync<TPage, TProps>(
        INavigation? navigation,
        Transition transition,
        Action<TProps> props)
        where TPage : Component, new()
        where TProps : class, new()
        => PushCore(
            navigation,
            transition,
            async () => await navigation.PushAsync<TPage, TProps>(animated: false, props)
                ?? throw new InvalidOperationException("Navigation.PushAsync returned no page."));

    public static async Task PopAsync(INavigation? navigation)
    {
        ArgumentNullException.ThrowIfNull(navigation);

        var context = HostContext.Current;
        if (context.IsBusy)
            return;

        if (navigation.NavigationStack.Count <= 1)
            return;

        context.IsBusy = true;
        try
        {
            var clip = context.PopClip();
            if (clip is not null)
                await clip.ReverseAsync();

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

            var flight = CreateHeroClip(page, hero, snapshot, transition);
            if (flight is not null)
                clips.Add(flight);
        }

        return clips.Count switch
        {
            0 => null,
            1 => clips[0],
            _ => Motion.Parallel([.. clips]),
        };
    }

    static IMotionClip? CreateHeroClip(
        MauiPage page,
        VisualElement hero,
        HeroSnapshot snapshot,
        Transition transition)
    {
        var destBounds = Geometry.GetWindowBounds(hero);
        if (destBounds.Width <= 0 || destBounds.Height <= 0)
            return null;

        var source = snapshot.WindowBounds;
        if (source.Width <= 0 || source.Height <= 0)
            return null;

        hero.BatchBegin();
        hero.AnchorX = 0;
        hero.AnchorY = 0;
        hero.TranslationX = source.X - destBounds.X;
        hero.TranslationY = source.Y - destBounds.Y;
        hero.ScaleX = source.Width / destBounds.Width;
        hero.ScaleY = source.Height / destBounds.Height;
        hero.BatchCommit();

        return Motion.On(hero)
            .Owner(page)
            .Duration(transition.Duration)
            .Easing(transition.Easing)
            .To(VisualElement.TranslationXProperty, 0d)
            .To(VisualElement.TranslationYProperty, 0d)
            .To(VisualElement.ScaleXProperty, 1d)
            .To(VisualElement.ScaleYProperty, 1d)
            .Build();
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
