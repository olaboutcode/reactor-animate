# Guarantees

From `HeroInFlight` through `HeroEnded`, the library:

- Reverts size, margin, alignment, and visibility changes on **flying** heroes.
- Defers unregistration of those heroes until the clip ends, so a re-render cannot drop them mid-flight.
- Keeps writing the transforms the clip owns, so setting `Translation` or `Scale` in a handler does not stick.

Non-hero chrome is not locked. `HeroEnded` runs after the lock is released.
