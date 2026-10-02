using Reactor.Animate;
using System.Runtime.CompilerServices;

namespace Reactor.Animate.Internals;

/// <summary>
/// One player owns a view. Registering a second player for the same view disposes
/// the previous one, including a run that is still in flight.
/// </summary>
internal static class MotionPlayers
{
    static readonly ConditionalWeakTable<VisualElement, MotionPlayer> Owners = [];
    static readonly Lock Gate = new();

    public static void Register(VisualElement view, MotionPlayer player)
    {
        MotionPlayer? stolen = null;
        lock (Gate)
        {
            if (Owners.TryGetValue(view, out var current) && !ReferenceEquals(current, player))
                stolen = current;
            Owners.AddOrUpdate(view, player);
        }

        if (stolen is null)
            return;

        System.Diagnostics.Debug.WriteLine("MotionPlayers: replacing player on view.");
        stolen.Dispose();
    }

    public static void Unregister(VisualElement view, MotionPlayer player)
    {
        lock (Gate)
        {
            if (Owners.TryGetValue(view, out var current) && ReferenceEquals(current, player))
                Owners.Remove(view);
        }
    }
}
