using Reactor.Animate;

namespace Reactor.Animate.Internals;

/// <summary>
/// Heroes that belong to the clip playing now. <see cref="HostContext"/> defers
/// unregister while a view is pinned, so a page unload during the flight does not
/// drop it. <see cref="TrackRuntime"/> skips pinned views so a motion does not
/// fight the clip for the same property.
/// </summary>
internal static class FlightPins
{
    static readonly HashSet<VisualElement> Pins = [];
    static readonly Lock Gate = new();

    public static void Pin(IReadOnlyList<VisualElement> heroes)
    {
        lock (Gate)
        {
            Pins.Clear();
            foreach (var hero in heroes)
                Pins.Add(hero);
        }
    }

    public static void Unpin()
    {
        lock (Gate)
            Pins.Clear();
    }

    public static bool IsPinned(VisualElement view)
    {
        lock (Gate)
            return Pins.Contains(view);
    }
}
