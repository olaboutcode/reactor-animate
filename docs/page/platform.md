# Platform

| Platform | What the host does |
|---|---|
| iOS, Mac Catalyst | Disables the interactive edge-swipe on `UINavigationController`. That gesture would pop with the platform slide during the drag. |
| Android | Intercepts system back and predictive back, then calls `Animate.Page.PopAsync`. On the root page, back still leaves the app. |

Hide the navigation bar on pages that fly (`HasNavigationBar(false)`). The host already handles back navigation.

Use MauiReactor `NavigationPage`, not Shell, for morphing pairs.
