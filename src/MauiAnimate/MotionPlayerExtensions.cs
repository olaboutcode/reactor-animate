namespace Reactor.Animate;

/// <summary>
/// Starts <see cref="MotionPlayer"/> playback. Safe on a null or disposed player.
/// </summary>
public static class MotionPlayerExtensions
{
    /// <summary>
    /// Starts forward. No-ops if <paramref name="player"/> is null or disposed.
    /// Does not throw on cancel.
    /// </summary>
    public static void Forward(this MotionPlayer? player)
        => _ = ForwardAsync(player);

    /// <summary>
    /// Starts reverse. No-ops if <paramref name="player"/> is null or disposed.
    /// Does not throw on cancel.
    /// </summary>
    public static void Reverse(this MotionPlayer? player)
        => _ = ReverseAsync(player);

    /// <summary>
    /// Awaits a forward run. Completes when the run finishes, is superseded,
    /// canceled, or <paramref name="player"/> is null. Never throws
    /// <see cref="OperationCanceledException"/> or <see cref="ObjectDisposedException"/>.
    /// </summary>
    public static Task ForwardAsync(this MotionPlayer? player, CancellationToken cancellationToken = default)
        => Quiet(() => player?.ForwardAsync(cancellationToken) ?? Task.CompletedTask);

    /// <summary>
    /// Awaits a reverse run. Same quiet contract as <see cref="ForwardAsync"/>.
    /// </summary>
    public static Task ReverseAsync(this MotionPlayer? player, CancellationToken cancellationToken = default)
        => Quiet(() => player?.ReverseAsync(cancellationToken) ?? Task.CompletedTask);

    static async Task Quiet(Func<Task> run)
    {
        try
        {
            await run();
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
