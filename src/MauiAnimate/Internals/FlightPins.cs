using Reactor.Animate;

namespace Reactor.Animate.Internals;

/// <summary>
/// Heroes that belong to the clip playing now. <see cref="HostContext"/> defers
/// unregister while a view is pinned, so a page unload during the flight does not
/// drop it. <see cref="TrackRuntime"/> skips pinned views so a motion does not
/// fight the clip for the same property.
/// <see cref="IsPinned"/> reads the count first, so a frame with no flight
/// takes no lock. The count is published only after the set changes. A pin
/// that skips the count lets a motion write on a pinned hero.
/// </summary>
internal static class FlightPins
{
    static readonly HashSet<VisualElement> Pins = [];
    static readonly Lock Gate = new();
    static int _count;

    public static void Pin(IReadOnlyList<VisualElement> heroes)
    {
        lock (Gate)
        {
            Pins.Clear();
            foreach (var hero in heroes)
                Pins.Add(hero);
            Volatile.Write(ref _count, Pins.Count);
        }
    }

    public static void Unpin()
    {
        lock (Gate)
        {
            Pins.Clear();
            Volatile.Write(ref _count, 0);
        }
    }

    public static bool IsPinned(VisualElement view)
    {
        if (Volatile.Read(ref _count) == 0)
            return false;

        lock (Gate)
            return Pins.Contains(view);
    }
}
