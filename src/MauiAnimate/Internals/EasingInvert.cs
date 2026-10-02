using Reactor.Animate;

namespace Reactor.Animate.Internals;

/// <summary>
/// Turns eased progress back into linear time for <see cref="MotionPlayer.SeekFraction"/>.
/// Linear, the cubic curves, and sine-in/out use a closed form. Other curves are
/// bisected. If that fails, the eased value is used as linear time.
/// </summary>
internal static class EasingInvert
{
    static bool _loggedFallback;

    public static bool TryInvert(Easing easing, double eased, out double u)
    {
        eased = double.IsNaN(eased) || double.IsInfinity(eased) ? 0 : Math.Clamp(eased, 0, 1);
        if (eased <= 0)
        {
            u = 0;
            return true;
        }

        if (eased >= 1)
        {
            u = 1;
            return true;
        }

        if (ReferenceEquals(easing, Easing.Linear))
        {
            u = eased;
            return true;
        }

        if (ReferenceEquals(easing, Easing.CubicIn))
        {
            u = Math.Cbrt(eased);
            return true;
        }

        if (ReferenceEquals(easing, Easing.CubicOut))
        {
            u = 1 - Math.Cbrt(1 - eased);
            return true;
        }

        if (ReferenceEquals(easing, Easing.CubicInOut))
        {
            u = eased < 0.5
                ? Math.Cbrt(eased / 4)
                : 1 - Math.Cbrt(2 * (1 - eased)) / 2;
            return true;
        }

        if (ReferenceEquals(easing, Easing.SinIn))
        {
            u = 2 / Math.PI * Math.Acos(1 - eased);
            return true;
        }

        if (ReferenceEquals(easing, Easing.SinOut))
        {
            u = 2 / Math.PI * Math.Asin(eased);
            return true;
        }

        if (TryBisect(easing, eased, out u))
            return true;

        if (!_loggedFallback)
        {
            _loggedFallback = true;
            System.Diagnostics.Debug.WriteLine("EasingInvert: falling back to linear u.");
        }

        u = eased;
        return false;
    }

    static bool TryBisect(Easing easing, double eased, out double u)
    {
        var lo = 0d;
        var hi = 1d;
        for (var i = 0; i < 24; i++)
        {
            var mid = (lo + hi) / 2;
            if (easing.Ease(mid) < eased)
                lo = mid;
            else
                hi = mid;
        }

        u = (lo + hi) / 2;
        return double.IsFinite(u);
    }
}
