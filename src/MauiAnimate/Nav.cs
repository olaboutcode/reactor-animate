using MauiReactor;
using MauiPage = Microsoft.Maui.Controls.Page;

namespace Reactor.Animate;

/// <summary>
/// Navigation helpers that play <see cref="Transition"/> clips instead of the platform slide.
/// </summary>
public static class Nav
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
            var originTags = OriginTags(transition);
            var snapshots = originTags
                .Select(context.Snapshot)
                .OfType<HeroSnapshot>()
                .ToArray();

            var page = await push();
            await WaitForLayout(page);
            await WaitForHeroes(transition, snapshots);

            var clip = BuildClip(page, transition, snapshots);
            if (clip is not null)
            {
                context.PushClip(clip);
                await clip.PlayAsync();
            }

            return page;
        }
        finally
        {
            context.IsBusy = false;
        }
    }

    static IEnumerable<string> OriginTags(Transition transition)
    {
        foreach (var tag in transition.HeroTags)
            yield return tag;

        if (transition.ExpandFromTag is { Length: > 0 } expand)
            yield return expand;
    }

    static IMotionClip? BuildClip(MauiPage page, Transition transition, HeroSnapshot[] snapshots)
    {
        var clips = new List<IMotionClip>();
        var snapshotByTag = snapshots
            .GroupBy(s => s.Tag, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.Ordinal);

        if (transition.ExpandFromTag is { Length: > 0 } expandTag &&
            snapshotByTag.TryGetValue(expandTag, out var expandSnapshot))
        {
            var expand = CreateExpandClip(page, expandSnapshot, transition);
            if (expand is not null)
                clips.Add(expand);
        }
        else
        {
            var pageClip = CreatePageClip(page, transition);
            if (pageClip is not null)
                clips.Add(pageClip);
        }

        // When the whole page expands from the source, heroes ride along.
        // A separate flight would double-move them.
        if (transition.ExpandFromTag is null)
        {
            foreach (var tag in transition.HeroTags)
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
        }

        return clips.Count switch
        {
            0 => null,
            1 => clips[0],
            _ => Motion.Parallel([.. clips]),
        };
    }

    static IMotionClip? CreateExpandClip(MauiPage page, HeroSnapshot snapshot, Transition transition)
    {
        var pageBounds = Geometry.GetWindowBounds(page);
        if (pageBounds.Width <= 0 || pageBounds.Height <= 0)
            return null;

        var source = snapshot.WindowBounds;
        page.AnchorX = 0;
        page.AnchorY = 0;
        page.TranslationX = source.X - pageBounds.X;
        page.TranslationY = source.Y - pageBounds.Y;
        page.ScaleX = source.Width / pageBounds.Width;
        page.ScaleY = source.Height / pageBounds.Height;

        var builder = Motion.On(page)
            .Owner(page)
            .Duration(transition.Duration)
            .Easing(transition.Easing)
            .To(VisualElement.TranslationXProperty, 0d)
            .To(VisualElement.TranslationYProperty, 0d)
            .To(VisualElement.ScaleXProperty, 1d)
            .To(VisualElement.ScaleYProperty, 1d);

        if (transition.PageEnter.HasFlag(PageEnter.Fade))
        {
            page.Opacity = 0;
            builder.To(VisualElement.OpacityProperty, 1d);
        }

        return builder.Build();
    }

    static IMotionClip? CreatePageClip(MauiPage page, Transition transition)
    {
        if (transition.PageEnter == PageEnter.None)
            return null;

        var builder = Motion.On(page)
            .Owner(page)
            .Duration(transition.Duration)
            .Easing(transition.Easing);

        var animating = false;

        if (transition.PageEnter.HasFlag(PageEnter.Fade))
        {
            page.Opacity = 0;
            builder.To(VisualElement.OpacityProperty, 1d);
            animating = true;
        }

        if (transition.PageEnter.HasFlag(PageEnter.SlideFromRight))
        {
            page.TranslationX = page.Width > 0 ? page.Width : 400;
            builder.To(VisualElement.TranslationXProperty, 0d);
            animating = true;
        }

        if (transition.PageEnter.HasFlag(PageEnter.SlideFromBottom))
        {
            page.TranslationY = page.Height > 0 ? page.Height : 800;
            builder.To(VisualElement.TranslationYProperty, 0d);
            animating = true;
        }

        if (transition.PageEnter.HasFlag(PageEnter.Scale))
        {
            page.Scale = 0.92;
            builder.To(VisualElement.ScaleProperty, 1d);
            animating = true;
        }

        return animating ? builder.Build() : null;
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
        hero.AnchorX = 0;
        hero.AnchorY = 0;
        hero.TranslationX = source.X - destBounds.X;
        hero.TranslationY = source.Y - destBounds.Y;
        hero.ScaleX = source.Width / destBounds.Width;
        hero.ScaleY = source.Height / destBounds.Height;

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
        if (page.Width > 0 && page.Height > 0)
        {
            await Task.Delay(16);
            return;
        }

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

        await Task.Delay(16);
    }

    static async Task WaitForHeroes(Transition transition, HeroSnapshot[] snapshots)
    {
        if (transition.HeroTags.Count == 0 || transition.ExpandFromTag is not null)
            return;

        var deadline = Environment.TickCount64 + 500;
        while (Environment.TickCount64 < deadline)
        {
            var ready = true;
            foreach (var tag in transition.HeroTags)
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
