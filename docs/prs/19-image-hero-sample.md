# PR 19 — Image → Image shared-element sample

## Goal

A sample pair where a `Image` in a list/grid flies to a larger `Image` on the detail page. PLAN called this out; Gallery currently uses `BoxView` tiles.

## Files

- `samples/Sample/Components/` — e.g. `PhotoPage` / `PhotoDetailPage`
- `samples/Sample/Components/HomePage.cs` — entry button
- Optional: `samples/Sample/Resources/Images/` — one or two MauiImages

## Behavior

- Same tag on both images (e.g. `"photo-{id}"`).
- Push: `t => t.Hero($"photo-{id}", h => h.AnchorCenter()).WithDuration(300)`
- Aspect-fit vs fill: dest image should match how FLIP scales the bounds; document if we clip.
- Reverse pop uses existing hero reverse.

## Merge gate

Hot-reload sample: tap thumbnail, image grows into detail, back shrinks to the cell. BoxView gallery still works.

## Depends on

None. Sample-only.
