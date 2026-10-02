using Reactor.Animate;

namespace Reactor.Animate.Internals;

/// <summary>
/// One app-wide registry. Holds tagged heroes, the navigation page from
/// <c>AnimateHost</c>, and the stack of flights a pop will reverse.
/// <see cref="HostContext.IsBusy"/> is true for the whole clip, including the
/// frame hold, so a second push returns the current page.
/// </summary>
internal sealed class HostContext
{
    public static HostContext Current { get; } = new();

    readonly Dictionary<string, List<VisualElement>> _heroes = [];
    readonly Stack<NavFlight> _flights = new();
    readonly List<(string Tag, VisualElement Element)> _deferredUnregister = [];
    readonly Lock _gate = new();

    public bool IsBusy { get; set; }

    public INavigation? Navigation { get; set; }

    public INavigation RequireNavigation()
        => Navigation
            ?? throw new InvalidOperationException("Wrap the root page with .AnimateHost() before calling Animate.Page.");

    public void RegisterHero(string tag, VisualElement element)
    {
        if (string.IsNullOrEmpty(tag))
            return;

        lock (_gate)
        {
            if (!_heroes.TryGetValue(tag, out var list))
                _heroes[tag] = list = [];

            if (!list.Contains(element))
                list.Add(element);
        }
    }

    public void UnregisterHero(string tag, VisualElement element)
    {
        lock (_gate)
        {
            if (FlightPins.IsPinned(element))
            {
                _deferredUnregister.Add((tag, element));
                return;
            }

            RemoveHero(tag, element);
        }
    }

    public void Pin(IReadOnlyList<VisualElement> heroes)
    {
        lock (_gate)
        {
            _deferredUnregister.Clear();
            FlightPins.Pin(heroes);
        }
    }

    public void Unpin()
    {
        lock (_gate)
        {
            FlightPins.Unpin();
            foreach (var (tag, element) in _deferredUnregister)
                RemoveHero(tag, element);
            _deferredUnregister.Clear();
        }
    }

    void RemoveHero(string tag, VisualElement element)
    {
        if (!_heroes.TryGetValue(tag, out var list))
            return;

        list.Remove(element);
        if (list.Count == 0)
            _heroes.Remove(tag);
    }

    public VisualElement? FindHero(string tag, VisualElement? excluding = null)
    {
        lock (_gate)
        {
            if (!_heroes.TryGetValue(tag, out var list))
                return null;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                var element = list[i];
                if (element != excluding && element.IsLoaded && element.Bounds.Width > 0 && element.Bounds.Height > 0)
                    return element;
            }

            return null;
        }
    }

    public VisualElement? FindHeroOn(string tag, Element root)
    {
        lock (_gate)
        {
            if (!_heroes.TryGetValue(tag, out var list))
                return null;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                var element = list[i];
                if (element.IsLoaded && Geometry.IsUnder(element, root))
                    return element;
            }

            return null;
        }
    }

    public HeroSnapshot? Snapshot(string tag)
    {
        var element = FindHero(tag);
        if (element is null)
            return null;

        return new HeroSnapshot(tag, Geometry.GetWindowBounds(element), element);
    }

    public void PushFlight(HeroTransition recipe, HeroSnapshot[] snapshots)
    {
        lock (_gate)
            _flights.Push(new NavFlight(recipe, snapshots));
    }

    public NavFlight? PopFlight()
    {
        lock (_gate)
            return _flights.Count > 0 ? _flights.Pop() : null;
    }
}

/// <summary>
/// Recipe and source snapshots kept on the stack so the matching pop can build
/// the return clip from the destination frames and negated extras.
/// </summary>
internal readonly record struct NavFlight(HeroTransition Recipe, HeroSnapshot[] Snapshots);

/// <summary>
/// Source hero measured at push: the tag, its window bounds, and the view those
/// bounds came from. The pop looks that view up again on the page being revealed.
/// </summary>
internal readonly record struct HeroSnapshot(string Tag, Rect WindowBounds, VisualElement Source);
