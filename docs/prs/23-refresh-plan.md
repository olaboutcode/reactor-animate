# PR 23 — Refresh PLAN.md to match shipped API

## Goal

`docs/PLAN.md` still describes v1 as page fade/slide/expand and “springs/path not v1”. README and the library already shipped `Animate.Motion` (including springs and path). Rewrite PLAN so it does not contradict README.

## Files

- `docs/PLAN.md` only

## Content

- Goal: shared-element **Page** + in-page **Motion**; expand-to-page later
- Out of scope: FluidNav router; whole-page recipes on `Animate.Page`
- Layers: `Animate.Page` / `Animate.Motion` as shipped
- Implementation order: mark PRs 1–16 done; point remaining work at PRs 17–22
- Do not mention `docs/LOCAL_SRS.md` or UC/FR ids

## Merge gate

No stale “fade/slide/scale page recipes” or “springs not v1” lines. README Status stays the source of truth for shipped features.

## Depends on

None. Docs-only.
