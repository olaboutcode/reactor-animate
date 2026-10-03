using Reactor.Animate;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Reactor.Animate.Internals;

/// <summary>
/// Projects a flat view rotating in depth. The entry matches Flutter
/// <c>Matrix4.setEntry(3, 2, entry)</c>: the camera is <c>1/entry</c>
/// device-independent pixels away. Positive Z comes toward the camera and
/// grows; the opposite edge shrinks.
/// </summary>
internal static class PerspectivePlane
{
#if IOS || MACCATALYST || ANDROID
    static readonly ConditionalWeakTable<VisualElement, State> Entries = new();
    static int _deferDepth;
    static VisualElement? _deferred;
    static bool _deferDirty;

    sealed class State
    {
        public double Entry;
        public Matrix4? Matrix;
        public bool Hooked;
    }
#endif

    /// <summary>
    /// Holds native reapplies for <paramref name="view"/> until
    /// <see cref="EndDeferReapply"/>. Matrix playback sets several pose
    /// properties, and each one would otherwise post its own reapply.
    /// </summary>
    public static void DeferReapply(VisualElement view)
    {
#if IOS || MACCATALYST || ANDROID
        if (_deferDepth == 0)
        {
            _deferred = view;
            _deferDirty = false;
        }

        _deferDepth++;
#else
        _ = view;
#endif
    }

    /// <summary>
    /// Posts one reapply when a deferred write changed the pose or the matrix.
    /// The handler assigns its own matrix after each property setter returns,
    /// so this post has to stay outside those setters.
    /// </summary>
    public static void EndDeferReapply()
    {
#if IOS || MACCATALYST || ANDROID
        if (_deferDepth == 0)
            return;

        _deferDepth--;
        if (_deferDepth > 0)
            return;

        var view = _deferred;
        var dirty = _deferDirty;
        _deferred = null;
        _deferDirty = false;
        if (dirty && view is not null)
            Queue(view);
#endif
    }

    public static (double X, double Y) Project(
        double x,
        double y,
        double rotationXDegrees,
        double rotationYDegrees,
        double perspectiveEntry)
    {
        var ry = rotationYDegrees * (Math.PI / 180.0);
        var rx = rotationXDegrees * (Math.PI / 180.0);
        var cosY = Math.Cos(ry);
        var sinY = Math.Sin(ry);
        var x1 = x * cosY;
        var z1 = x * sinY;

        var cosX = Math.Cos(rx);
        var sinX = Math.Sin(rx);
        var y2 = y * cosX - z1 * sinX;
        var z2 = y * sinX + z1 * cosX;

        var w = 1.0 - z2 * perspectiveEntry;
        if (w is > -1e-4 and < 1e-4)
            w = w < 0 ? -1e-4 : 1e-4;
        return (x1 / w, y2 / w);
    }

    /// <summary>
    /// Android <c>SetCameraDistance</c> argument that matches
    /// <c>CATransform3D.m34 = -entry</c>. The platform divides the argument by
    /// <c>densityDpi</c>, and the resulting camera is not in dips, so the dip
    /// eye distance (<c>1/|entry|</c>) is multiplied by <c>density² × √5</c>.
    /// </summary>
    internal static float AndroidCameraDistance(double entry, float density)
    {
        if (density <= 0 || float.IsNaN(density) || float.IsInfinity(density))
            density = 1;

        // 0 is orthographic: a camera far enough that the plane does not foreshorten.
        var eyeDips = entry == 0 ? 1_000_000d : 1.0 / Math.Abs(entry);
        return (float)(density * density * eyeDips * Math.Sqrt(5.0));
    }

    public static void Apply(VisualElement view, double perspectiveEntry)
    {
#if IOS || MACCATALYST || ANDROID
        var state = Entries.GetOrCreateValue(view);
        state.Matrix = null;
        state.Entry = perspectiveEntry;
        Hook(view);
        PlatformApply(view);
#else
        _ = view;
        _ = perspectiveEntry;
#endif
    }

    public static void Apply(VisualElement view, Matrix4 matrix)
    {
#if IOS || MACCATALYST || ANDROID
        var state = Entries.GetOrCreateValue(view);
        state.Matrix = matrix;
        state.Entry = MatrixMaps.PerspectiveStrength(matrix);
        Hook(view);
        if (_deferDepth > 0 && ReferenceEquals(view, _deferred))
        {
            _deferDirty = true;
            return;
        }

        PlatformApply(view);
#else
        _ = view;
        _ = matrix;
#endif
    }

#if IOS || MACCATALYST || ANDROID
    static void Hook(VisualElement view)
    {
        var state = Entries.GetOrCreateValue(view);
        if (state.Hooked)
            return;

        state.Hooked = true;
        view.PropertyChanged += OnPropertyChanged;
        view.SizeChanged += OnSizeChanged;
        view.HandlerChanged += OnHandlerChanged;
    }
#endif

#if IOS || MACCATALYST || ANDROID
    static void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not VisualElement view)
            return;
        if (e.PropertyName is not (
            nameof(VisualElement.Rotation)
            or nameof(VisualElement.RotationX)
            or nameof(VisualElement.RotationY)
            or nameof(VisualElement.TranslationX)
            or nameof(VisualElement.TranslationY)
            or nameof(VisualElement.Scale)
            or nameof(VisualElement.ScaleX)
            or nameof(VisualElement.ScaleY)
            or nameof(VisualElement.AnchorX)
            or nameof(VisualElement.AnchorY)
            or nameof(VisualElement.Width)
            or nameof(VisualElement.Height)))
        {
            return;
        }

        // The handler assigns its own matrix after PropertyChanged returns.
        // A matrix write sets several of these properties. One post covers them.
        if (_deferDepth > 0 && ReferenceEquals(sender, _deferred))
        {
            _deferDirty = true;
            return;
        }

        Queue(view);
    }

    static void OnSizeChanged(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
            Queue(view);
    }

    static void OnHandlerChanged(object? sender, EventArgs e)
    {
        if (sender is VisualElement view)
            Queue(view);
    }

    static void Queue(VisualElement view)
    {
        var dispatcher = view.Dispatcher;
        if (dispatcher is null)
        {
            PlatformApply(view);
            return;
        }

        dispatcher.Dispatch(() => PlatformApply(view));
    }

    static void PlatformApply(VisualElement view)
    {
        if (!Entries.TryGetValue(view, out var state))
            return;

#if IOS || MACCATALYST
        if (state.Matrix is { } matrix)
            ApplyAppleMatrix(view, matrix);
        else
            ApplyApple(view, state.Entry);
#elif ANDROID
        ApplyAndroid(view, state.Entry);
#endif
    }
#endif

#if IOS || MACCATALYST
    static void ApplyApple(VisualElement view, double entry)
    {
        if (view.Handler?.PlatformView is not UIKit.UIView platform)
            return;

        var width = view.Frame.Width;
        var height = view.Frame.Height;
        if (width <= 0 || height <= 0)
            return;

        const double epsilon = 0.001;
        var anchorX = view.AnchorX;
        var anchorY = view.AnchorY;
        var transform = CoreAnimation.CATransform3D.Identity;

        // Same composition as MAUI UpdateTransformation. M34 replaces the
        // handler's fixed -1/400 so the recipe owns the eye distance.
        if (Math.Abs(anchorX - 0.5) > epsilon)
            transform = transform.Translate((nfloat)((anchorX - 0.5) * width), 0, 0);
        if (Math.Abs(anchorY - 0.5) > epsilon)
            transform = transform.Translate(0, (nfloat)((anchorY - 0.5) * height), 0);
        if (Math.Abs(view.TranslationX) > epsilon || Math.Abs(view.TranslationY) > epsilon)
            transform = transform.Translate((nfloat)view.TranslationX, (nfloat)view.TranslationY, 0);

        transform.M34 = (nfloat)(-entry);

        if (Math.Abs(view.RotationX % 360) > epsilon)
            transform = transform.Rotate((nfloat)(view.RotationX * Math.PI / 180.0), (nfloat)1, 0, 0);
        if (Math.Abs(view.RotationY % 360) > epsilon)
            transform = transform.Rotate((nfloat)(view.RotationY * Math.PI / 180.0), 0, (nfloat)1, 0);
        transform = transform.Rotate((nfloat)(view.Rotation * Math.PI / 180.0), 0, 0, (nfloat)1);

        var scaleX = view.ScaleX * view.Scale;
        var scaleY = view.ScaleY * view.Scale;
        if (Math.Abs(scaleX - 1) > epsilon || Math.Abs(scaleY - 1) > epsilon)
            transform = transform.Scale((nfloat)scaleX, (nfloat)scaleY, (nfloat)view.Scale);

        var layer = platform.Layer;
        if (layer is null)
            return;

        layer.AnchorPoint = new CoreGraphics.CGPoint(anchorX, anchorY);
        layer.Transform = transform;
    }

    static void ApplyAppleMatrix(VisualElement view, Matrix4 matrix)
    {
        if (view.Handler?.PlatformView is not UIKit.UIView platform)
            return;

        var width = view.Frame.Width;
        var height = view.Frame.Height;
        if (width <= 0 || height <= 0)
            return;

        var layer = platform.Layer;
        if (layer is null)
            return;

        const double epsilon = 0.001;
        var anchorX = view.AnchorX;
        var anchorY = view.AnchorY;
        var user = ToCATransform(matrix);
        var transform = CoreAnimation.CATransform3D.Identity;
        if (Math.Abs(anchorX - 0.5) > epsilon)
            transform = transform.Translate((nfloat)((anchorX - 0.5) * width), 0, 0);
        if (Math.Abs(anchorY - 0.5) > epsilon)
            transform = transform.Translate(0, (nfloat)((anchorY - 0.5) * height), 0);
        transform = transform.Concat(user);

        layer.AnchorPoint = new CoreGraphics.CGPoint(anchorX, anchorY);
        layer.Transform = transform;
    }

    static CoreAnimation.CATransform3D ToCATransform(Matrix4 matrix)
    {
        var transform = CoreAnimation.CATransform3D.Identity;
        transform.M11 = (nfloat)MatrixMaps.RowMajor(matrix, 0, 0);
        transform.M12 = (nfloat)MatrixMaps.RowMajor(matrix, 0, 1);
        transform.M13 = (nfloat)MatrixMaps.RowMajor(matrix, 0, 2);
        transform.M14 = (nfloat)MatrixMaps.RowMajor(matrix, 0, 3);
        transform.M21 = (nfloat)MatrixMaps.RowMajor(matrix, 1, 0);
        transform.M22 = (nfloat)MatrixMaps.RowMajor(matrix, 1, 1);
        transform.M23 = (nfloat)MatrixMaps.RowMajor(matrix, 1, 2);
        transform.M24 = (nfloat)MatrixMaps.RowMajor(matrix, 1, 3);
        transform.M31 = (nfloat)MatrixMaps.RowMajor(matrix, 2, 0);
        transform.M32 = (nfloat)MatrixMaps.RowMajor(matrix, 2, 1);
        transform.M33 = (nfloat)MatrixMaps.RowMajor(matrix, 2, 2);
        transform.M34 = (nfloat)MatrixMaps.RowMajor(matrix, 2, 3);
        transform.M41 = (nfloat)MatrixMaps.RowMajor(matrix, 3, 0);
        transform.M42 = (nfloat)MatrixMaps.RowMajor(matrix, 3, 1);
        transform.M43 = (nfloat)MatrixMaps.RowMajor(matrix, 3, 2);
        transform.M44 = (nfloat)MatrixMaps.RowMajor(matrix, 3, 3);
        return transform;
    }
#endif

#if ANDROID
    static void ApplyAndroid(VisualElement view, double entry)
    {
        if (view.Handler?.PlatformView is not Android.Views.View platform)
            return;

        var density = platform.Resources?.DisplayMetrics?.Density ?? 1f;
        // Rotation lives on the wrapper when MAUI has inserted one.
        var target = platform.Parent is Microsoft.Maui.Platform.WrapperView wrapper
            ? wrapper
            : platform;
        target.SetCameraDistance(AndroidCameraDistance(entry, density));
    }
#endif
}
