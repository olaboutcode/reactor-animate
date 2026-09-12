namespace Reactor.Animate;

sealed class HostContext
{
    public static HostContext Current { get; } = new();

    readonly Dictionary<string, List<VisualElement>> _heroes = [];
    readonly Stack<IMotionClip> _clips = new();
    readonly object _gate = new();

    public bool IsBusy { get; set; }

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
            if (!_heroes.TryGetValue(tag, out var list))
                return;

            list.Remove(element);
            if (list.Count == 0)
                _heroes.Remove(tag);
        }
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
                if (element != excluding && element.IsLoaded && element.Width > 0 && element.Height > 0)
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

    public void PushClip(IMotionClip clip)
    {
        lock (_gate)
            _clips.Push(clip);
    }

    public IMotionClip? PopClip()
    {
        lock (_gate)
            return _clips.Count > 0 ? _clips.Pop() : null;
    }
}

readonly record struct HeroSnapshot(string Tag, Rect WindowBounds, VisualElement Source);
