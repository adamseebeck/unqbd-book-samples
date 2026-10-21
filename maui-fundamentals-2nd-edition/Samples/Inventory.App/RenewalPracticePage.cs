using MauiInventory.Core;

namespace MauiInventory.App;
public sealed class RenewalPracticePage : ContentPage
{
    private readonly Label status = new()
    {
        Text = "Controlled renewal; no real identity provider is contacted."
    };
    private readonly PracticeClock clock = new();
    private readonly Switch fail = new();
    private readonly AccessTokenCoordinator coordinator;
    private CancellationTokenSource? appearance;
    public RenewalPracticePage()
    {
        Title = "Token renewal practice";
        SemanticProperties.SetDescription(fail, "Simulate a renewal failure");
        coordinator = new(async token =>
        {
            await Task.Delay(750, token);
            if (fail.IsToggled)
                throw new HttpRequestException("Controlled renewal failure.");
            return new AccessSession("fixed-local-exercise-value", clock.GetUtcNow().AddMinutes(10));
        }, clock);
        var read = new Button
        {
            Text = "Get a usable demonstration session"
        };
        read.Clicked += async (_, _) =>
        {
            if (appearance is null)
                return;
            var token = appearance.Token;
            read.IsEnabled = false;
            try
            {
                var result = await coordinator.GetAsync(token);
                if (!token.IsCancellationRequested)
                    status.Text = $"Session expires {result.ExpiresAt:O}. Token content is hidden.";
            }
            catch (OperationCanceledException)when (token.IsCancellationRequested)
            {
            }
            catch (Exception)
            {
                if (!token.IsCancellationRequested)
                    status.Text = "Renewal unavailable. Observe the 30-second cooldown before trying again.";
            }
            finally
            {
                read.IsEnabled = true;
            }
        };
        var advance = new Button
        {
            Text = "Advance the exercise clock by 6 minutes"
        };
        advance.Clicked += (_, _) =>
        {
            clock.Offset += TimeSpan.FromMinutes(6);
            status.Text = "Exercise clock advanced. The next read enters the 5-minute renewal window.";
        };
        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 12,
            Children =
            {
                status,
                new Label
                {
                    Text = "Simulate renewal failure"
                },
                fail,
                read,
                advance
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        appearance?.Dispose();
        appearance = new();
    }

    protected override void OnDisappearing()
    {
        appearance?.Cancel();
        base.OnDisappearing();
    }

    private sealed class PracticeClock : TimeProvider
    {
        public TimeSpan Offset;
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow + Offset;
    }
}
