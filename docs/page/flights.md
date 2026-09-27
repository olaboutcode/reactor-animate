# Flights

```
push                                         pop
snapshot source                              snapshot dest heroes
hold (source)                                hold (dest)
push dest                                    pop dest
invert dest → source frames                  invert source → dest frames
  + extras (Rotate, Translate)                 + negated extras
HeroInFlight, hold lifts                     HeroInFlight, hold lifts
play dest to rest                            play source to rest
```

FLIP: the incoming view is laid out at rest, then translated and scaled to cover the outgoing frame. The clip plays those transforms back to rest. Push extras (`Rotate`, `Translate`) are negated on pop. Compatible bindable properties (color, corner radius) interpolate with the motion.

Non-hero content on the incoming page fades in after the shared-element clip is more than halfway through.

Defaults: **400 ms**, **`Easing.CubicOut`**.

The library animates the **frame**. `WithAnimation` / `MotionPlayer` animate the **insides**.
