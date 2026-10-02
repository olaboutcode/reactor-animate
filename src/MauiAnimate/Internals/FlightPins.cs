using Reactor.Animate;

namespace Reactor.Animate.Internals;

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
