## Reactor.Animate Documentation

An in depth functional reference to all library features

### Table of contents
 - [Overview](#overview)
 - [Hero Animation](#hero-animation)
   - [Animate.Page](#animate.page)

### Overview

Reactor.Animate provides a two ways of adding animations to your app.
- `Animate.Page` for connected element, page to page animations also referred to as hero animations.
- `Animate.Motion` for on-page, individual element(s) animations.

The library offers control over those animations. The can be played forward, reversed, pause, and rest. The library also provides ways to hook into animation lifecyle. For example you can add a listener for when an animation is about to start, in-progress, or has finished.
### Hero Animation
#### Animate.Page