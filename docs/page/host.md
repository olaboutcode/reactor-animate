# Host

```csharp
new HomePage().AnimateHost();
```

| Member | Role |
|---|---|
| `VisualNode.AnimateHost()` | Wraps the tree in `AnimatedHost`. Prefer this over constructing the host directly. |
| `AnimatedHost` | Renders a `NavigationPage`, suppresses platform transitions, and wires back handling. |

Call this once at the app root. `Animate.Page` throws if the host is missing.

```
new HomePage().AnimateHost()   // once, around NavigationPage
  ├── NavigationPage
  │     ├── ListPage
  │     │     Image(...).Hero("cover")
  │     └── DetailPage
  │           Image(...).Hero("cover")
  └── Overlay / hold frame     // in-flight
```
