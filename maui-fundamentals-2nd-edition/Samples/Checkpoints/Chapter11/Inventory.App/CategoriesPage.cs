using MauiInventory.Core;

namespace MauiInventory.App;
public sealed class AppTextFileStore : ITextFileStore
{
    public Task WriteAsync(string name, string contents, CancellationToken token)
    {
        if (name != "categories.csv")
            throw new ArgumentException("Unsupported export file.", nameof(name));
        return File.WriteAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, name), contents, new System.Text.UTF8Encoding(false), token);
    }
}

public sealed class CategoriesPage : ContentPage
{
    private readonly CategoryViewModel vm;
    private bool active;
    private int visit;
    public CategoriesPage(CategoryViewModel vm)
    {
        this.vm = vm;
        Title = "Categories";
        BindingContext = vm;
        var name = new Entry
        {
            Placeholder = "New category name"
        };
        name.SetBinding(Entry.TextProperty, nameof(vm.NewName));
        var description = new Entry
        {
            Placeholder = "Category description"
        };
        description.SetBinding(Entry.TextProperty, nameof(vm.NewDescription));
        var status = new Label();
        status.SetBinding(Label.TextProperty, nameof(vm.ErrorMessage));
        var add = new Button
        {
            Text = "Add category"
        };
        var export = new Button
        {
            Text = "Export categories"
        };
        add.SetBinding(Button.CommandProperty, nameof(vm.AddCommand));
        export.SetBinding(Button.CommandProperty, nameof(vm.ExportCommand));
        var timestamp = new Label();
        timestamp.SetBinding(Label.TextProperty, nameof(vm.LastExportUtc), stringFormat: "Last export: {0:O}");
        var location = new Label
        {
            Text = $"Export file: {Path.Combine(FileSystem.AppDataDirectory, "categories.csv")}"
        };
        var list = new CollectionView
        {
            ItemsSource = vm.Categories,
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label();
                label.SetBinding(Label.TextProperty, nameof(Category.Name));
                return label;
            })
        };
        Content = new Grid
        {
            Padding = 16,
            RowDefinitions = [new(GridLength.Auto), new(GridLength.Star)],
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        name,
                        description,
                        add,
                        export,
                        status,
                        timestamp,
                        location
                    }
                },
                list.AtRow(1)
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        active = true;
        var current = ++visit;
        try
        {
            var pending = new[]
            {
                vm.LoadCommand.ExecutionTask,
                vm.AddCommand.ExecutionTask,
                vm.ExportCommand.ExecutionTask
            };
            foreach (var operation in pending.OfType<Task>().Distinct())
                try
                {
                    await operation;
                }
                catch (OperationCanceledException)
                {
                }

            if (active && current == visit && vm.LoadCommand.CanExecute(null))
                await vm.LoadCommand.ExecuteAsync(null);
        }
        catch (Exception)
        {
            if (active && current == visit)
                await DisplayAlertAsync("Loading unavailable", "Try reopening categories.", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        active = false;
        visit++;
        vm.LoadCommand.Cancel();
        vm.AddCommand.Cancel();
        vm.ExportCommand.Cancel();
        base.OnDisappearing();
    }
}

public sealed class ExportSettings : IExportSettings
{
    public void SetLastExport(DateTimeOffset timestamp) => Preferences.Default.Set("LastExportUtc", timestamp.ToString("O"));
}
