# Navigation comparisons

These are small study alternatives to the registered workflow, not additional routes already installed in AppShell. Use a separate scratch page to compare their contracts.

## Attribute assignment and dictionary receipt

QueryProperty assigns an incoming value through a named public property. This example stores only an identifier; its setter does not start unobserved asynchronous work. The attribute uses reflection and is not trim safe. Prefer IQueryAttributable for the book's trimming/AOT preparation, and validate before loading either way.

```csharp
[QueryProperty(nameof(ProductId), "id")]
public sealed class AttributeDetailPage : ContentPage
{
    public string ProductId { get; set; } = "";
}

public sealed class DictionaryDetailPage : ContentPage, IQueryAttributable
{
    public int? ProductId { get; private set; }
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ProductId = query.TryGetValue("id", out var raw) &&
            int.TryParse(raw?.ToString(), out var id) && id > 0 ? id : null;
    }
}
```

Register a selected scratch page with Routing.RegisterRoute before its first relative GoToAsync("scratch-detail?id=1") call. Shell string-query values and object dictionaries have different decoding/type contracts; do not cast every object to string or decode an already-decoded attribute value again. Inspect DetailsPage and ProductDetailViewModel for the supplied asynchronous initialization and replacement/cancellation behavior.

## Register and exercise one constrained path

In a scratch copy of the default Inventory.App Shell, add this registration once in its constructor. Use the default app, whose actual hierarchy is inventory/catalog/products; Activity07 instead has an activity/exercise root. The dictionary page above receives id as a string even when the constraint matched. Its separate positive-ID validation rejects -1.

```csharp
Routing.RegisterRoute("study-product/{id:int}", typeof(DictionaryDetailPage));

// From an awaited UI handler after the default Shell exists:
await Shell.Current.GoToAsync("//inventory/catalog/products/study-product/42");
try
{
    await Shell.Current.GoToAsync("//inventory/catalog/products/study-product/abc");
}
catch (ArgumentException)
{
    // No matching integer-constrained route. Keep the current screen usable.
}
```

The registered template is a study alternative; keep the original relative details route installed for the ordinary sample. Route-template matching uses an absolute hierarchy URI. A matching integer controls route selection, not authorization, positivity or conversion to an int object. See the primary [Shell navigation reference](https://learn.microsoft.com/dotnet/maui/fundamentals/shell/navigation).

## Await one modal owner and its result

The following dedicated modal has no child navigation. Its owner awaits presentation, then the result task. The button awaits dismissal before completing a chosen result; ordinary back dismissal completes null through OnDisappearing. The closing flag prevents that callback from replacing the button's result during its own pop. A failed pop leaves the modal open and retryable. Keep this example on the UI thread. A production flow also needs an owner-level cancellation/window-close policy; no in-memory task survives process termination.

```csharp
public sealed class ChoiceModal : ContentPage
{
    private readonly TaskCompletionSource<string?> completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool closing;
    public Task<string?> Result => completion.Task;

    public ChoiceModal()
    {
        var choose = new Button { Text = "Use this choice" };
        choose.Clicked += async (_, _) =>
        {
            if (closing) return;
            closing = true;
            choose.IsEnabled = false;
            try
            {
                await Navigation.PopModalAsync();
                completion.TrySetResult("chosen");
            }
            catch (Exception)
            {
                closing = false;
                choose.IsEnabled = true;
                await DisplayAlertAsync("Still open", "Dismissal failed. Try again.", "OK");
            }
        };
        Content = choose;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (!closing) completion.TrySetResult(null);
    }
}

// Call from an awaited async handler on the owner page:
var modal = new ChoiceModal();
await Navigation.PushModalAsync(modal);
string? result = await modal.Result;
// Apply result only while this owner is still the intended active screen.
```

## Inspect Android destruction without making it a save hook

Temporarily add this override inside the supplied Platforms/Android/MainActivity class, beside OnActivityResult. Remove it when the experiment ends. Inspect the debugger output for an actual finish and a configuration-triggered recreation. The manifest handles several configuration changes, so a normal rotation may not destroy this activity. Change only a scratch manifest or use a real unhandled configuration change to study recreation. Force-stop/process death may skip OnDestroy entirely.

```csharp
protected override void OnDestroy()
{
    System.Diagnostics.Debug.WriteLine(
        $"Activity destroy: finishing={IsFinishing}, " +
        $"changingConfigurations={IsChangingConfigurations}");
    base.OnDestroy();
}
```

These flags explain an observed callback. They do not promise a final callback, durable persistence, or restoration of a transaction after process loss.
