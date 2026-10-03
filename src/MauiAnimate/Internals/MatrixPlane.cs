using Reactor.Animate;

namespace Reactor.Animate.Internals;

/// <summary>
/// Writes one <see cref="Matrix4"/> onto a view. iOS and Mac Catalyst assign the
/// 4×4 to the layer. Android sets camera distance from the perspective row and
/// writes translation, rotation, and scale as view properties. The pivot is the
/// view's <see cref="VisualElement.AnchorX"/> and <see cref="VisualElement.AnchorY"/>.
/// </summary>
internal static class MatrixPlane
{
    public static void Apply(VisualElement view, Matrix4 matrix)
    {
#if WINDOWS
        ApplyWindows(view, matrix);
#else
        // Property setters make MAUI replace the native matrix. Defer collects
        // those changes and queues one reapply after the pose is written.
        PerspectivePlane.DeferReapply(view);
        try
        {
            PerspectivePlane.Apply(view, matrix);
            var pose = MatrixPose.From(matrix);
            view.Scale = 1;
            view.TranslationX = pose.TranslationX;
            view.TranslationY = pose.TranslationY;
            view.Rotation = pose.RotationZ;
            view.RotationX = pose.RotationX;
            view.RotationY = pose.RotationY;
            view.ScaleX = pose.ScaleX;
            view.ScaleY = pose.ScaleY;
        }
        finally
        {
            PerspectivePlane.EndDeferReapply();
        }
#endif
    }

#if WINDOWS
    static void ApplyWindows(VisualElement view, Matrix4 matrix)
    {
        if (view.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement element)
            return;

        var width = view.Frame.Width > 0 ? view.Frame.Width : view.Width;
        var height = view.Frame.Height > 0 ? view.Frame.Height : view.Height;
        var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(element);
        visual.CenterPoint = new System.Numerics.Vector3(
            (float)(view.AnchorX * width),
            (float)(view.AnchorY * height),
            0);
        visual.TransformMatrix = new System.Numerics.Matrix4x4(
            (float)MatrixMaps.RowMajor(matrix, 0, 0),
            (float)MatrixMaps.RowMajor(matrix, 0, 1),
            (float)MatrixMaps.RowMajor(matrix, 0, 2),
            (float)MatrixMaps.RowMajor(matrix, 0, 3),
            (float)MatrixMaps.RowMajor(matrix, 1, 0),
            (float)MatrixMaps.RowMajor(matrix, 1, 1),
            (float)MatrixMaps.RowMajor(matrix, 1, 2),
            (float)MatrixMaps.RowMajor(matrix, 1, 3),
            (float)MatrixMaps.RowMajor(matrix, 2, 0),
            (float)MatrixMaps.RowMajor(matrix, 2, 1),
            (float)MatrixMaps.RowMajor(matrix, 2, 2),
            (float)MatrixMaps.RowMajor(matrix, 2, 3),
            (float)MatrixMaps.RowMajor(matrix, 3, 0),
            (float)MatrixMaps.RowMajor(matrix, 3, 1),
            (float)MatrixMaps.RowMajor(matrix, 3, 2),
            (float)MatrixMaps.RowMajor(matrix, 3, 3));
    }
#endif
}
