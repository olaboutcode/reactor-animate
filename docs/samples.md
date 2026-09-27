# Samples

The sample app (`samples/Sample`) is a MauiReactor gallery.

| Page | What it shows |
|---|---|
| Home heroes | Several tagged boxes, per-tag anchors and rotation |
| Gallery | CollectionView cell → detail hero |
| Photos | Image cell → detail hero (same `Source`) |
| Circle to top | Orb hero plus dest chrome via `BindMotion` |
| Motion playground | Fade, slide, scale, pulse, yoyo, bounce, keyframes, intro, spring pop |
| Stagger grid | 12 tiles, grid stagger |
| Scrub | Slider → `SeekFraction` |
| Color HSV vs RGB | Same red→lime, two color spaces |
| Timeline seek | Named `Add` / `Then` + `Seek(id)` |
| Path | Bézier and `ArcSegment` lanes |

CI (`.github/workflows/ci.yml`) runs `MauiAnimate.Tests` on Ubuntu (`net10.0`) and builds the library for `net10.0-android` and `net10.0-ios`. No simulator or device run.
