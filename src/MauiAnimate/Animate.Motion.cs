using MotionRecipe = Reactor.Animate.Animation.Motion;

namespace Reactor.Animate;

public static partial class Animate
{
    /// <summary>
    /// In-page view motion. Recipes are immutable; playback lives on
    /// <see cref="MotionPlayer"/>. Sibling of <see cref="Page"/>.
    /// </summary>
    public static partial class Motion
    {
        public static MotionRecipe Define(Func<MotionRecipe, MotionRecipe> configure)
            => configure(MotionRecipe.None);

        public static MotionPlayer Bind(MotionRecipe motion, VisualElement target)
            => Bind(motion, (IEnumerable<VisualElement>)[target]);

        public static MotionPlayer Bind(MotionRecipe motion, IEnumerable<VisualElement> targets)
        {
            ArgumentNullException.ThrowIfNull(motion);
            ArgumentNullException.ThrowIfNull(targets);
            return MotionPlayer.Create(motion, targets as IReadOnlyList<VisualElement> ?? [.. targets]);
        }

        public static MotionPlayer Bind(Func<MotionRecipe, MotionRecipe> configure, VisualElement target)
            => Bind(configure(MotionRecipe.None), target);

        public static MotionPlayer Bind(Func<MotionRecipe, MotionRecipe> configure, IEnumerable<VisualElement> targets)
            => Bind(configure(MotionRecipe.None), targets);

        public static MotionPlayer Play(MotionRecipe motion, params VisualElement[] targets)
        {
            var player = Bind(motion, targets);
            player.Forward();
            return player;
        }

        public static MotionPlayer Play(Func<MotionRecipe, MotionRecipe> configure, VisualElement target)
            => Play(configure(MotionRecipe.None), target);

        public static MotionPlayer Play(Func<MotionRecipe, MotionRecipe> configure, IEnumerable<VisualElement> targets)
            => Play(configure(MotionRecipe.None), [.. targets]);

        public static Task ForwardAsync(
            MotionRecipe motion,
            VisualElement target,
            CancellationToken cancellationToken = default)
            => MotionPlayerExtensions.ForwardAsync(Bind(motion, target), cancellationToken);

        public static Task ForwardAsync(
            MotionRecipe motion,
            IEnumerable<VisualElement> targets,
            CancellationToken cancellationToken = default)
            => MotionPlayerExtensions.ForwardAsync(Bind(motion, targets), cancellationToken);

        public static Task ForwardAsync(
            Func<MotionRecipe, MotionRecipe> configure,
            VisualElement target,
            CancellationToken cancellationToken = default)
            => ForwardAsync(configure(MotionRecipe.None), target, cancellationToken);

        public static Task ForwardAsync(
            Func<MotionRecipe, MotionRecipe> configure,
            IEnumerable<VisualElement> targets,
            CancellationToken cancellationToken = default)
            => ForwardAsync(configure(MotionRecipe.None), targets, cancellationToken);

        public static Task ReverseAsync(
            MotionRecipe motion,
            VisualElement target,
            CancellationToken cancellationToken = default)
            => MotionPlayerExtensions.ReverseAsync(Bind(motion, target), cancellationToken);

        public static Task ReverseAsync(
            MotionRecipe motion,
            IEnumerable<VisualElement> targets,
            CancellationToken cancellationToken = default)
            => MotionPlayerExtensions.ReverseAsync(Bind(motion, targets), cancellationToken);

        public static Task ReverseAsync(
            Func<MotionRecipe, MotionRecipe> configure,
            VisualElement target,
            CancellationToken cancellationToken = default)
            => ReverseAsync(configure(MotionRecipe.None), target, cancellationToken);

        public static Task ReverseAsync(
            Func<MotionRecipe, MotionRecipe> configure,
            IEnumerable<VisualElement> targets,
            CancellationToken cancellationToken = default)
            => ReverseAsync(configure(MotionRecipe.None), targets, cancellationToken);
    }
}
