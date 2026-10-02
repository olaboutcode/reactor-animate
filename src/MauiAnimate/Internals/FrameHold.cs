using Reactor.Animate;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace Reactor.Animate.Internals;

// Screenshot overlay held across the navigation change so the new page does not flash
// before the clip starts. Release clears it.
internal static class FrameHold
{
    static HoldOverlay? _overlay;

    public static async Task CaptureAsync()
    {
        if (!Screenshot.Default.IsCaptureSupported)
            return;

        var window = Application.Current?.Windows.OfType<Window>().FirstOrDefault();
        if (window is null)
            return;

        var shot = await Screenshot.Default.CaptureAsync();
        await using var stream = await shot.OpenReadAsync();
        IImage image = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(stream);
        var longest = Math.Max(image.Width, image.Height);
        if (longest > 720)
            image = image.Downsize(720, disposeOriginal: true);

        void Show()
        {
            if (_overlay is null || !window.Overlays.Contains(_overlay))
            {
                _overlay = new HoldOverlay(window);
                window.AddOverlay(_overlay);
            }

            _overlay.SetImage(image);
        }

        if (MainThread.IsMainThread)
            Show();
        else
            await MainThread.InvokeOnMainThreadAsync(Show);
    }

    public static void Release()
        => _overlay?.SetImage(null);
}

// Window overlay that draws the FrameHold screenshot.
internal sealed class HoldOverlay : WindowOverlay
{
    readonly HoldElement _element = new();

    public HoldOverlay(IWindow window) : base(window)
    {
        AddWindowElement(_element);
    }

    public void SetImage(IImage? image)
    {
        _element.Image = image;
        IsVisible = image is not null;
        Invalidate();
    }
}

// Full-window image for HoldOverlay. Contains is true while an image is showing.
internal sealed class HoldElement : IWindowOverlayElement
{
    public IImage? Image { get; set; }

    public bool Contains(Point point) => Image is not null;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Image is null)
            return;

        canvas.DrawImage(Image, dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);
    }
}
