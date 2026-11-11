namespace MauiInventory.App;
public partial class ResourcePracticePage : ContentPage
{
    public ResourcePracticePage() => InitializeComponent();
    private async void LoadImage(object? sender, EventArgs e)
    {
        if (sender is not Button button || !button.IsEnabled)
            return;
        button.IsEnabled = false;
        try
        {
            using var source = await FileSystem.OpenAppPackageFileAsync("sample-thumbnail.png");
            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer);
            var bytes = buffer.ToArray();
            Preview.Source = ImageSource.FromStream(() => new MemoryStream(bytes, writable: false));
            ImageStatus.Text = "Loaded the packaged image. The control sizes how it is shown, not the file itself.";
        }
        catch (Exception)
        {
            ImageStatus.Text = "Image unavailable for this target. The text remains usable.";
        }
        finally
        {
            button.IsEnabled = true;
        }
    }
}
