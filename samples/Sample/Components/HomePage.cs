using Reactor.Animate;

namespace Sample.Components;

sealed class HomePage : Component
{
    public override VisualNode Render()
        => ContentPage(
            ScrollView(
                VStack(
                    new Hero("box")
                    {
                        BoxView()
                            .WidthRequest(100)
                            .HeightRequest(100)
                            .BackgroundColor(Colors.CornflowerBlue)
                            .HCenter()
                    },

                    Label("Reactor.Animate")
                        .FontSize(28)
                        .HCenter(),

                    Label("Shared element, expand-from-element, and whole-page transitions.")
                        .FontSize(16)
                        .HCenter(),

                    Button("Shared element", async () => await OpenHero()),
                    Button("Expand from element", async () => await OpenExpand()),
                    Button("Whole page fade", async () => await OpenFade()),
                    Button("Expand + fade", async () => await OpenExpandFade()),
                    Button("Slide from bottom", async () => await OpenSlide())
                )
                .Spacing(16)
                .Padding(24)
            )
        )
        .HasNavigationBar(false);

    Task<MauiControls.Page> OpenHero()
        => Nav.PushAsync<DetailPage, DetailProps>(
            Navigation,
            Transition.Hero("box"),
            props =>
            {
                props.Title = "Shared element";
                props.Body = "The box flies from the list into this page.";
                props.ShowHero = true;
            });

    Task<MauiControls.Page> OpenExpand()
        => Nav.PushAsync<DetailPage, DetailProps>(
            Navigation,
            Transition.ExpandFrom("box"),
            props =>
            {
                props.Title = "Expand from element";
                props.Body = "This page grows out of the box.";
                props.ShowHero = true;
            });

    Task<MauiControls.Page> OpenFade()
        => Nav.PushAsync<DetailPage, DetailProps>(
            Navigation,
            Transition.Page(PageEnter.Fade),
            props =>
            {
                props.Title = "Whole page fade";
                props.Body = "No shared element. The page fades in as a unit.";
                props.ShowHero = false;
            });

    Task<MauiControls.Page> OpenExpandFade()
        => Nav.PushAsync<DetailPage, DetailProps>(
            Navigation,
            Transition.ExpandFrom("box") | Transition.Page(PageEnter.Fade),
            props =>
            {
                props.Title = "Expand + fade";
                props.Body = "The page grows from the box and fades in.";
                props.ShowHero = true;
            });

    Task<MauiControls.Page> OpenSlide()
        => Nav.PushAsync<DetailPage, DetailProps>(
            Navigation,
            Transition.Page(PageEnter.SlideFromBottom | PageEnter.Fade),
            props =>
            {
                props.Title = "Slide from bottom";
                props.Body = "Whole-page slide + fade.";
                props.ShowHero = false;
            });
}
