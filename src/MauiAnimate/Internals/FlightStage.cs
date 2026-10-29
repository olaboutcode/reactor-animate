using Microsoft.Maui.Platform;
using Reactor.Animate;

namespace Reactor.Animate.Internals;

/// <summary>
/// Picture of a view, taken while it is still on screen. A pop takes one
/// before the destination page is removed. <see cref="Detach"/> hands the
/// picture to the stage, which releases it.
/// </summary>
internal sealed class FlightPlate : IDisposable
{
    object? _native;
    bool _disposed;

    public static FlightPlate? Take(VisualElement view)
    {
        var native = Shoot(view);
        return native is null ? null : new FlightPlate(native);
    }

    FlightPlate(object native) => _native = native;

    public object? Detach()
    {
        var native = _native;
        _native = null;
        return native;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var native = _native;
        _native = null;
        Release(native);
    }

    static object? Shoot(VisualElement view)
    {
#if IOS || MACCATALYST || ANDROID
        return FlightStage.Shoot(view);
#else
        _ = view;
        return null;
#endif
    }

    static void Release(object? native)
    {
#if IOS || MACCATALYST
        if (native is UIKit.UIView uiView)
        {
            uiView.RemoveFromSuperview();
            uiView.Dispose();
        }
#elif ANDROID
        if (native is Android.Graphics.Bitmap bitmap && !bitmap.IsRecycled)
            bitmap.Recycle();
#else
        _ = native;
#endif
    }
}

/// <summary>
/// Draws the flight in a window overlay. The real hero stays in its scroll,
/// with no translation and no ancestor unclip, so the scroll cannot paint
/// into the status bar. The overlay crossfades a picture of each end.
/// </summary>
internal sealed class FlightStage : IDisposable
{
    readonly uint _duration;
    readonly Easing _easing;
    readonly MotionClock _clock = new();
    readonly TaskCompletionSource<bool> _done = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<Card> _cards = [];
    readonly List<Hidden> _hidden = [];
    readonly List<object> _owned = [];
    double _elapsed;
    double _shown;
    int _finished;
    bool _disposed;
    bool _placing;

    FlightStage(uint duration, Easing easing)
    {
        _duration = duration;
        _easing = easing;
    }

    public static FlightStage? Open(IReadOnlyList<FlightLeg> legs, uint duration, Easing easing)
    {
        if (legs.Count == 0)
            return null;

#if IOS || MACCATALYST || ANDROID
        var stage = new FlightStage(duration, easing);
        if (!stage.TryBuild(legs))
        {
            stage.Dispose();
            return null;
        }

        stage.Apply(0);
        return stage;
#else
        _ = duration;
        _ = easing;
        return null;
#endif
    }

    public void Apply(double progress)
    {
        if (_disposed || _placing)
            return;

        _shown = double.IsNaN(progress) ? 0 : Math.Clamp(progress, 0, 1);
        _placing = true;
        try
        {
            foreach (var card in _cards)
            {
                var pose = FlightPose.At(card.Leg.From, card.Leg.To, card.Leg.Extras, card.Leg.RestRotation, _shown);
                Place(card, pose, _shown);
            }
        }
        finally
        {
            _placing = false;
        }
    }

    public Task PlayAsync(Action<double>? report)
    {
        Apply(0);
        if (_cards.Count == 0 || _duration == 0)
        {
            Apply(1);
            report?.Invoke(1);
            return Task.CompletedTask;
        }

        _clock.Connect(delta => OnDelta(delta, report));
        var targets = new VisualElement[_cards.Count];
        for (var i = 0; i < _cards.Count; i++)
            targets[i] = _cards[i].Leg.Flying;

        _clock.Start(targets);
        if (!_clock.IsPumping)
        {
            Apply(1);
            report?.Invoke(1);
            return Task.CompletedTask;
        }

        return _done.Task;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _clock.Stop();
        Finish(canceled: true);

        for (var i = _hidden.Count - 1; i >= 0; i--)
        {
            var hidden = _hidden[i];
            try
            {
                hidden.View.Opacity = hidden.Opacity;
                hidden.View.Handler?.UpdateValue(nameof(VisualElement.Opacity));
            }
            catch (ObjectDisposedException)
            {
            }
        }

        _hidden.Clear();
        TearDown();
        _cards.Clear();
        for (var i = _owned.Count - 1; i >= 0; i--)
            ReleaseOwned(_owned[i]);

        _owned.Clear();
    }

    void OnDelta(double delta, Action<double>? report)
    {
        if (_disposed || Volatile.Read(ref _finished) != 0)
            return;

        if (double.IsPositiveInfinity(delta) || _duration == 0)
        {
            Apply(1);
            report?.Invoke(1);
            Finish(canceled: false);
            return;
        }

        _elapsed += delta;
        if (_elapsed >= _duration)
        {
            Apply(1);
            report?.Invoke(1);
            Finish(canceled: false);
            return;
        }

        var eased = _easing.Ease(Math.Clamp(_elapsed / _duration, 0, 1));
        Apply(eased);
        report?.Invoke(eased);
    }

    void Finish(bool canceled)
    {
        if (Interlocked.Exchange(ref _finished, 1) != 0)
            return;

        _clock.Stop();
        if (canceled)
            _done.TrySetCanceled();
        else
            _done.TrySetResult(true);
    }

    static void ReleaseOwned(object native)
    {
#if IOS || MACCATALYST
        if (native is UIKit.UIView uiView)
        {
            uiView.RemoveFromSuperview();
            uiView.Dispose();
        }
#elif ANDROID
        if (native is Android.Graphics.Bitmap bitmap && !bitmap.IsRecycled)
            bitmap.Recycle();
#else
        _ = native;
#endif
    }

    readonly record struct Hidden(VisualElement View, double Opacity);

    sealed class Card
    {
        public required FlightLeg Leg { get; init; }

        public object? Start { get; init; }

        public object? End { get; init; }

        public object? Host { get; init; }
    }

#if IOS || MACCATALYST || ANDROID
    internal static object? Shoot(VisualElement view)
        => Capture(PlatformOf(view));

    static object? PlatformOf(VisualElement view)
        => view.Handler?.PlatformView
            ?? (view.Handler?.MauiContext is { } context ? view.ToPlatform(context) : null);

    void Hide(VisualElement view)
    {
        _hidden.Add(new Hidden(view, view.Opacity));
        view.Opacity = 0;
        view.Handler?.UpdateValue(nameof(VisualElement.Opacity));
    }

    void Keep(object? native)
    {
        if (native is not null)
            _owned.Add(native);
    }
#else
    static void Place(Card card, FlightPose.Sample pose, double progress)
    {
        _ = card;
        _ = pose;
        _ = progress;
    }

    void TearDown()
        => _ = _cards.Count;
#endif

#if IOS || MACCATALYST
    UIKit.UIView? _root;

    bool TryBuild(IReadOnlyList<FlightLeg> legs)
    {
        foreach (var leg in legs)
        {
            var start = Picture(leg.StartPlate, leg.StartLook);
            var end = Capture(PlatformOf(leg.Flying)) as UIKit.UIView;
            if (start is null && end is null)
                continue;

            var flying = PlatformOf(leg.Flying) as UIKit.UIView;
            var window = flying?.Window ?? end?.Window ?? start?.Window;
            if (window is null)
            {
                Drop(start);
                Drop(end);
                continue;
            }

            _root ??= AddRoot(window);
            var host = new UIKit.UIView
            {
                UserInteractionEnabled = false,
                ClipsToBounds = false,
                BackgroundColor = UIKit.UIColor.Clear,
            };
            if (start is not null)
                host.AddSubview(start);
            if (end is not null)
                host.AddSubview(end);

            _root.AddSubview(host);
            Keep(start);
            Keep(end);
            _cards.Add(new Card { Leg = leg, Host = host, Start = start, End = end });
            Hide(leg.Flying);
        }

        return _cards.Count > 0;
    }

    static UIKit.UIView AddRoot(UIKit.UIWindow window)
    {
        var root = new UIKit.UIView(window.Bounds)
        {
            UserInteractionEnabled = false,
            ClipsToBounds = false,
            AutoresizingMask = UIKit.UIViewAutoresizing.FlexibleWidth | UIKit.UIViewAutoresizing.FlexibleHeight,
            BackgroundColor = UIKit.UIColor.Clear,
        };
        window.AddSubview(root);
        return root;
    }

    static UIKit.UIView? Picture(FlightPlate? plate, VisualElement? look)
    {
        if (plate?.Detach() is UIKit.UIView detached)
            return detached;

        return look is null ? null : Capture(PlatformOf(look)) as UIKit.UIView;
    }

    static object? Capture(object? native)
    {
        if (native is not UIKit.UIView view || view.Bounds.Width <= 0 || view.Bounds.Height <= 0)
            return null;

        view.Superview?.LayoutIfNeeded();
        var shot = view.SnapshotView(afterScreenUpdates: false);
        if (shot is not null)
            return shot;

        var format = new UIKit.UIGraphicsImageRendererFormat
        {
            Opaque = false,
            Scale = view.Window?.Screen.Scale ?? 0,
        };
        var renderer = new UIKit.UIGraphicsImageRenderer(view.Bounds.Size, format);
        var image = renderer.CreateImage(context => view.Layer.RenderInContext(context.CGContext));
        return new UIKit.UIImageView(image)
        {
            ContentMode = UIKit.UIViewContentMode.ScaleToFill,
        };
    }

    static void Place(Card card, FlightPose.Sample pose, double progress)
    {
        if (card.Host is not UIKit.UIView host)
            return;

        var width = (nfloat)Math.Max(pose.Width, 0);
        var height = (nfloat)Math.Max(pose.Height, 0);
        var x = (nfloat)pose.X;
        var y = (nfloat)pose.Y;
        if (host.Superview is { } parent && parent.Window is { } window)
        {
            var local = parent.ConvertPointFromView(new CoreGraphics.CGPoint(x, y), window);
            x = (nfloat)local.X;
            y = (nfloat)local.Y;
        }

        host.Bounds = new CoreGraphics.CGRect(0, 0, width, height);
        host.Layer.AnchorPoint = new CoreGraphics.CGPoint((nfloat)pose.AnchorX, (nfloat)pose.AnchorY);
        host.Layer.Position = new CoreGraphics.CGPoint(
            x + width * (nfloat)pose.AnchorX,
            y + height * (nfloat)pose.AnchorY);
        host.Layer.Transform = pose.Rotation == 0
            ? CoreAnimation.CATransform3D.Identity
            : CoreAnimation.CATransform3D.MakeRotation((nfloat)(pose.Rotation * Math.PI / 180d), 0, 0, 1);

        var bounds = host.Bounds;
        Show(card.Start as UIKit.UIView, bounds, card.End is null ? 1d : 1d - progress);
        Show(card.End as UIKit.UIView, bounds, card.Start is null ? 1d : progress);
    }

    static void Show(UIKit.UIView? view, CoreGraphics.CGRect bounds, double alpha)
    {
        if (view is null)
            return;

        view.Frame = bounds;
        view.Alpha = (nfloat)alpha;
    }

    void TearDown()
    {
        _root?.RemoveFromSuperview();
        _root?.Dispose();
        _root = null;
    }

    static void Drop(UIKit.UIView? view)
    {
        view?.RemoveFromSuperview();
        view?.Dispose();
    }
#elif ANDROID
    StageRoot? _root;
    float _density = 1;
    int _originX;
    int _originY;

    bool TryBuild(IReadOnlyList<FlightLeg> legs)
    {
        foreach (var leg in legs)
        {
            var start = Picture(leg.StartPlate, leg.StartLook);
            var end = Capture(PlatformOf(leg.Flying));
            if (start is null && end is null)
                continue;

            var flying = PlatformOf(leg.Flying) as Android.Views.View;
            var decor = DecorOf(flying);
            if (decor is null || flying?.Context is not { } context)
            {
                Drop(start);
                Drop(end);
                continue;
            }

            if (_root is null)
            {
                var density = flying.Resources?.DisplayMetrics?.Density ?? 1;
                if (density > 0)
                    _density = density;

                var origin = new int[2];
                decor.GetLocationOnScreen(origin);
                _originX = origin[0];
                _originY = origin[1];
                _root = AddRoot(decor);
            }

            var host = new Android.Widget.FrameLayout(context);
            host.SetClipChildren(false);
            host.SetClipToPadding(false);
            host.Clickable = false;
            Android.Views.View? startView = null;
            Android.Views.View? endView = null;
            if (start is not null)
            {
                startView = Image(context, start);
                host.AddView(startView, Match());
            }

            if (end is not null)
            {
                endView = Image(context, end);
                host.AddView(endView, Match());
            }

            _root.AddView(host);
            Keep(start);
            Keep(end);
            _cards.Add(new Card { Leg = leg, Host = host, Start = startView, End = endView });
            Hide(leg.Flying);
        }

        return _cards.Count > 0;
    }

    StageRoot AddRoot(Android.Views.ViewGroup decor)
    {
        var root = new StageRoot(this, decor.Context!);
        decor.AddView(root, Match());
        root.BringToFront();
        return root;
    }

    static Android.Widget.FrameLayout.LayoutParams Match()
        => new(
            Android.Views.ViewGroup.LayoutParams.MatchParent,
            Android.Views.ViewGroup.LayoutParams.MatchParent);

    static Android.Widget.ImageView Image(Android.Content.Context context, Android.Graphics.Bitmap bitmap)
    {
        var image = new Android.Widget.ImageView(context);
        image.SetScaleType(Android.Widget.ImageView.ScaleType.FitXy);
        image.SetImageBitmap(bitmap);
        return image;
    }

    static Android.Graphics.Bitmap? Picture(FlightPlate? plate, VisualElement? look)
    {
        if (plate?.Detach() is Android.Graphics.Bitmap detached)
            return detached;

        return look is null ? null : Capture(PlatformOf(look));
    }

    static Android.Graphics.Bitmap? Capture(object? native)
    {
        if (native is not Android.Views.View view || view.Width <= 0 || view.Height <= 0)
            return null;

        var bitmap = Android.Graphics.Bitmap.CreateBitmap(
            view.Width,
            view.Height,
            Android.Graphics.Bitmap.Config.Argb8888!);
        if (bitmap is null)
            return null;

        view.Draw(new Android.Graphics.Canvas(bitmap));
        return bitmap;
    }

    static Android.Views.ViewGroup? DecorOf(Android.Views.View? view)
    {
        var context = view?.Context;
        while (context is Android.Content.ContextWrapper wrapper)
        {
            if (wrapper is Android.App.Activity activity)
                return activity.Window?.DecorView as Android.Views.ViewGroup;

            context = wrapper.BaseContext;
        }

        return null;
    }

    void Place(Card card, FlightPose.Sample pose, double progress)
    {
        if (card.Host is not Android.Views.View host)
            return;

        var width = Math.Max(Px(pose.Width), 0);
        var height = Math.Max(Px(pose.Height), 0);
        var left = Px(pose.X) - _originX;
        var top = Px(pose.Y) - _originY;
        host.Layout(left, top, left + width, top + height);
        host.PivotX = width * (float)pose.AnchorX;
        host.PivotY = height * (float)pose.AnchorY;
        host.Rotation = (float)pose.Rotation;
        Show(card.Start as Android.Views.View, width, height, card.End is null ? 1d : 1d - progress);
        Show(card.End as Android.Views.View, width, height, card.Start is null ? 1d : progress);
    }

    static void Show(Android.Views.View? view, int width, int height, double alpha)
    {
        if (view is null)
            return;

        view.Layout(0, 0, width, height);
        view.Alpha = (float)alpha;
    }

    int Px(double dip)
        => (int)Math.Round(dip * _density);

    void TearDown()
    {
        _root?.RemoveFromParent();
        _root?.Dispose();
        _root = null;
    }

    static void Drop(Android.Graphics.Bitmap? bitmap)
    {
        if (bitmap is not null && !bitmap.IsRecycled)
            bitmap.Recycle();
    }

    sealed class StageRoot : Android.Widget.FrameLayout
    {
        readonly FlightStage _stage;

        public StageRoot(FlightStage stage, Android.Content.Context context)
            : base(context)
        {
            _stage = stage;
            Clickable = false;
            SetClipChildren(false);
            SetClipToPadding(false);
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
            => _stage.Apply(_stage._shown);
    }
#else
#endif
}
