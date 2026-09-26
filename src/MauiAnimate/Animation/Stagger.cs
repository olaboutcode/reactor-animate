namespace Reactor.Animate.Animation;

public enum StaggerFrom
{
    Start,
    Center,
    End,
}

public readonly record struct Stagger(
    uint StepMilliseconds,
    StaggerFrom From = StaggerFrom.Start,
    (int Columns, int Rows)? Grid = null);
