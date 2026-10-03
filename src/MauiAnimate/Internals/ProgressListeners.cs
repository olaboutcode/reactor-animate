namespace Reactor.Animate.Internals;

/// <summary>
/// Progress callbacks for one run. A tick reuses the last snapshot, so playback
/// does not allocate an array per frame. A callback added during a tick is
/// invoked on the next tick, the same as copying the list before walking it.
/// </summary>
internal sealed class ProgressListeners
{
    readonly List<Action<double>> _items = [];
    Action<double>[] _snapshot = [];
    bool _dirty = true;

    public void Add(Action<double> callback)
    {
        _items.Add(callback);
        _dirty = true;
    }

    public ReadOnlySpan<Action<double>> Snapshot()
    {
        if (!_dirty)
            return _snapshot;

        _snapshot = _items.Count == 0 ? [] : _items.ToArray();
        _dirty = false;
        return _snapshot;
    }
}
