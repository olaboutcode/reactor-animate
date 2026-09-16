using System.ComponentModel;

namespace Reactor.Animate.Page;

/// <summary>
/// Freezes layout on flying heroes for the clip. User code that changes size,
/// margin, visibility, or options mid-flight is reverted until dispose.
/// Transforms the tween owns are left alone.
/// </summary>
internal sealed class FlightLock : IDisposable
{
    static readonly BindableProperty[] LayoutProperties =
    [
        VisualElement.WidthRequestProperty,
        VisualElement.HeightRequestProperty,
        VisualElement.MinimumWidthRequestProperty,
        VisualElement.MinimumHeightRequestProperty,
        VisualElement.MaximumWidthRequestProperty,
        VisualElement.MaximumHeightRequestProperty,
        VisualElement.IsVisibleProperty,
        View.MarginProperty,
        View.HorizontalOptionsProperty,
        View.VerticalOptionsProperty,
    ];

    readonly List<Entry> _entries = [];
    bool _reverting;
    bool _disposed;

    public FlightLock(IEnumerable<VisualElement> heroes)
    {
        foreach (var hero in heroes)
        {
            var original = new Dictionary<BindableProperty, object?>();
            foreach (var property in LayoutProperties)
                original[property] = hero.GetValue(property);

            var bounds = hero.Bounds;
            var frozen = new Dictionary<BindableProperty, object?>(original);
            if (bounds.Width > 0 && bounds.Height > 0)
            {
                _reverting = true;
                try
                {
                    hero.WidthRequest = bounds.Width;
                    hero.HeightRequest = bounds.Height;
                    hero.Handler?.UpdateValue(nameof(VisualElement.WidthRequest));
                    hero.Handler?.UpdateValue(nameof(VisualElement.HeightRequest));
                }
                finally
                {
                    _reverting = false;
                }

                frozen[VisualElement.WidthRequestProperty] = hero.WidthRequest;
                frozen[VisualElement.HeightRequestProperty] = hero.HeightRequest;
            }

            hero.PropertyChanged += OnPropertyChanged;
            hero.SizeChanged += OnSizeChanged;
            _entries.Add(new Entry(hero, original, frozen, bounds));
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _reverting = true;
        try
        {
            foreach (var entry in _entries)
            {
                entry.Hero.PropertyChanged -= OnPropertyChanged;
                entry.Hero.SizeChanged -= OnSizeChanged;
                foreach (var (property, value) in entry.Restore)
                {
                    if (Equals(entry.Hero.GetValue(property), value))
                        continue;
                    entry.Hero.SetValue(property, value);
                    entry.Hero.Handler?.UpdateValue(property.PropertyName);
                }
            }
        }
        finally
        {
            _reverting = false;
            _entries.Clear();
        }
    }

    void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_reverting || sender is not VisualElement hero || e.PropertyName is null)
            return;

        foreach (var entry in _entries)
        {
            if (!ReferenceEquals(entry.Hero, hero))
                continue;

            foreach (var (property, value) in entry.Frozen)
            {
                if (property.PropertyName != e.PropertyName)
                    continue;
                if (Equals(hero.GetValue(property), value))
                    return;

                _reverting = true;
                try
                {
                    hero.SetValue(property, value);
                    hero.Handler?.UpdateValue(property.PropertyName);
                }
                finally
                {
                    _reverting = false;
                }

                return;
            }
        }
    }

    void OnSizeChanged(object? sender, EventArgs e)
    {
        if (_reverting || sender is not VisualElement hero)
            return;

        foreach (var entry in _entries)
        {
            if (!ReferenceEquals(entry.Hero, hero))
                continue;
            if (entry.Bounds.Width <= 0 || entry.Bounds.Height <= 0)
                return;
            if (Math.Abs(hero.Bounds.Width - entry.Bounds.Width) < 0.5
                && Math.Abs(hero.Bounds.Height - entry.Bounds.Height) < 0.5)
                return;

            _reverting = true;
            try
            {
                hero.WidthRequest = entry.Bounds.Width;
                hero.HeightRequest = entry.Bounds.Height;
                hero.Handler?.UpdateValue(nameof(VisualElement.WidthRequest));
                hero.Handler?.UpdateValue(nameof(VisualElement.HeightRequest));
            }
            finally
            {
                _reverting = false;
            }

            return;
        }
    }

    readonly record struct Entry(
        VisualElement Hero,
        Dictionary<BindableProperty, object?> Restore,
        Dictionary<BindableProperty, object?> Frozen,
        Rect Bounds);
}
