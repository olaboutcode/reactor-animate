using MotionRecipe = Reactor.Animate.Motion;

namespace Reactor.Animate;

public static partial class Animate
{
    /// <summary>
    /// In-page view motion. Recipes are immutable; playback lives on
    /// <see cref="MotionPlayer"/>. Sibling of <see cref="Page"/>.
    /// </summary>
    public static partial class Motion
    {
        /// <summary>
        /// Builds a recipe by applying <paramref name="configure"/> to an empty motion.
        /// </summary>
        public static MotionRecipe Define(Func<MotionRecipe, MotionRecipe> configure)
            => configure(MotionRecipe.None);

        /// <summary>
        /// Binds <paramref name="motion"/> to <paramref name="target"/>. Does not start playback.
        /// </summary>
        public static MotionPlayer Bind(MotionRecipe motion, VisualElement target)
            => Bind(motion, [target]);

        /// <summary>
        /// Binds <paramref name="motion"/> to <paramref name="targets"/>. Does not start playback.
        /// </summary>
        public static MotionPlayer Bind(
            MotionRecipe motion,
            IEnumerable<VisualElement> targets)
            => MotionPlayer.Create(motion, targets as IReadOnlyList<VisualElement> ?? [.. targets]);

        /// <summary>
        /// Builds a recipe from an empty motion, then binds it to <paramref name="target"/>.
        /// Does not start playback.
        /// </summary>
        public static MotionPlayer Bind(
            Func<MotionRecipe, MotionRecipe> configure,
            VisualElement target)
            => Bind(configure(MotionRecipe.None), target);

        /// <summary>
        /// Builds a recipe from an empty motion, then binds it to <paramref name="targets"/>.
        /// Does not start playback.
        /// </summary>
        public static MotionPlayer Bind(
            Func<MotionRecipe, MotionRecipe> configure,
            IEnumerable<VisualElement> targets)
            => Bind(configure(MotionRecipe.None), targets);

        /// <summary>
        /// Binds <paramref name="motion"/> and starts it forward.
        /// Keep the player to pause or reverse.
        /// </summary>
        public static MotionPlayer Play(MotionRecipe motion, params VisualElement[] targets)
        {
            var player = Bind(motion, targets);
            player.Forward();
            return player;
        }

        /// <summary>
        /// Builds a recipe from an empty motion, binds it to <paramref name="target"/>,
        /// and starts it forward.
        /// </summary>
        public static MotionPlayer Play(
            Func<MotionRecipe, MotionRecipe> configure,
            VisualElement target)
            => Play(configure(MotionRecipe.None), target);

        /// <summary>
        /// Builds a recipe from an empty motion, binds it to <paramref name="targets"/>,
        /// and starts it forward.
        /// </summary>
        public static MotionPlayer Play(
            Func<MotionRecipe, MotionRecipe> configure,
            IEnumerable<VisualElement> targets)
            => Play(configure(MotionRecipe.None), [.. targets]);

        /// <summary>
        /// Binds <paramref name="motion"/> to <paramref name="target"/> and plays it forward.
        /// A later bind on the same view replaces this run. Cancellation does not throw.
        /// </summary>
        public static Task ForwardAsync(
            MotionRecipe motion,
            VisualElement target,
            CancellationToken cancellationToken = default)
            => MotionPlayerExtensions.ForwardAsync(Bind(motion, target), cancellationToken);

        /// <summary>
        /// Binds <paramref name="motion"/> to <paramref name="targets"/> and plays it forward.
        /// A later bind on the same view replaces this run. Cancellation does not throw.
        /// </summary>
        public static Task ForwardAsync(
            MotionRecipe motion,
            IEnumerable<VisualElement> targets,
            CancellationToken cancellationToken = default)
            => MotionPlayerExtensions.ForwardAsync(Bind(motion, targets), cancellationToken);

        /// <summary>
        /// Builds a recipe from an empty motion, binds it to <paramref name="target"/>,
        /// and plays it forward. Cancellation does not throw.
        /// </summary>
        public static Task ForwardAsync(
            Func<MotionRecipe, MotionRecipe> configure,
            VisualElement target,
            CancellationToken cancellationToken = default)
            => ForwardAsync(configure(MotionRecipe.None), target, cancellationToken);

        /// <summary>
        /// Builds a recipe from an empty motion, binds it to <paramref name="targets"/>,
        /// and plays it forward. Cancellation does not throw.
        /// </summary>
        public static Task ForwardAsync(
            Func<MotionRecipe, MotionRecipe> configure,
            IEnumerable<VisualElement> targets,
            CancellationToken cancellationToken = default)
            => ForwardAsync(configure(MotionRecipe.None), targets, cancellationToken);

        /// <summary>
        /// Binds <paramref name="motion"/> to <paramref name="target"/> and plays it in reverse.
        /// A later bind on the same view replaces this run. Cancellation does not throw.
        /// </summary>
        public static Task ReverseAsync(
            MotionRecipe motion,
            VisualElement target,
            CancellationToken cancellationToken = default)
            => MotionPlayerExtensions.ReverseAsync(Bind(motion, target), cancellationToken);

        /// <summary>
        /// Binds <paramref name="motion"/> to <paramref name="targets"/> and plays it in reverse.
        /// A later bind on the same view replaces this run. Cancellation does not throw.
        /// </summary>
        public static Task ReverseAsync(
            MotionRecipe motion,
            IEnumerable<VisualElement> targets,
            CancellationToken cancellationToken = default)
            => MotionPlayerExtensions.ReverseAsync(Bind(motion, targets), cancellationToken);

        /// <summary>
        /// Builds a recipe from an empty motion, binds it to <paramref name="target"/>,
        /// and plays it in reverse. Cancellation does not throw.
        /// </summary>
        public static Task ReverseAsync(
            Func<MotionRecipe, MotionRecipe> configure,
            VisualElement target,
            CancellationToken cancellationToken = default)
            => ReverseAsync(configure(MotionRecipe.None), target, cancellationToken);

        /// <summary>
        /// Builds a recipe from an empty motion, binds it to <paramref name="targets"/>,
        /// and plays it in reverse. Cancellation does not throw.
        /// </summary>
        public static Task ReverseAsync(
            Func<MotionRecipe, MotionRecipe> configure,
            IEnumerable<VisualElement> targets,
            CancellationToken cancellationToken = default)
            => ReverseAsync(configure(MotionRecipe.None), targets, cancellationToken);
    }
}
